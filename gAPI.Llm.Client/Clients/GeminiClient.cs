using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using gAPI.Llm.Client.Interfaces;
using gAPI.Llm.Client.Models;
using gAPI.Llm.Client.Enums;

namespace gAPI.Llm.Client.Clients;

public class GeminiClient : ILlmClient
{
    private readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(3600) };
    private readonly string ApiKey;
    private readonly string BaseUrl;

    public bool Initialized { get; set; }

    public GeminiClient(string? apiKey = null, string? baseUrl = null)
    {
        ApiKey = apiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        BaseUrl = baseUrl ?? "https://generativelanguage.googleapis.com";
        Initialized = !string.IsNullOrWhiteSpace(ApiKey);
    }

    public async Task<Model[]> GetModels(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return GetDefaultModels();
        }

        try
        {
            var url = new Uri($"{BaseUrl}/v1beta/models?key={ApiKey}");
            var response = await HttpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return GetDefaultModels();

            var data = await response.Content.ReadFromJsonAsync<GeminiModelListResponse>(cancellationToken: ct);
            if (data?.models == null)
                return GetDefaultModels();

            var models = new List<Model>();
            foreach (var m in data.models)
            {
                var name = m.name?.Replace("models/", "") ?? "";
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                models.Add(new Model(
                    name,
                    0, // Memory size not provided by Gemini list API
                    m.inputTokenLimit ?? 1048576,
                    DateTime.UtcNow
                ));
            }
            return [.. models];
        }
        catch
        {
            return GetDefaultModels();
        }
    }

    private static Model[] GetDefaultModels()
    {
        return new Model[]
        {
            new("gemini-1.5-flash", 0, 1048576, DateTime.UtcNow),
            new("gemini-1.5-pro", 0, 2097152, DateTime.UtcNow),
            new("gemini-2.0-flash", 0, 1048576, DateTime.UtcNow),
            new("gemini-2.5-flash", 0, 1048576, DateTime.UtcNow)
        };
    }

    public async Task InitializeModelAsync(Model model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Please set the GEMINI_API_KEY environment variable or pass it to the constructor.");
        }
        Initialized = true;
        await Task.CompletedTask;
    }

    public async Task<LlmResponse> ChatAsync(Model model, LlmRequest apiCall, CancellationToken ct = default, LlmOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Please set the GEMINI_API_KEY environment variable.");
        }

        string payload = CreateRequestJson(model, apiCall, options);

        var modelName = model.Name;
        if (!modelName.StartsWith("models/"))
        {
            modelName = $"models/{modelName}";
        }

        var url = new Uri($"{BaseUrl}/v1beta/{modelName}:generateContent?key={ApiKey}");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        response.EnsureSuccessStatusCode();

        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(json, DefaultJsonSerializerOptions.JsonDeserializerOptions)
            ?? throw new Exception("Failed to deserialize Gemini API response.");

        var candidate = geminiResponse.candidates?.FirstOrDefault() ?? throw new Exception("No candidates returned from Gemini API.");
        var parts = candidate.content?.parts ?? Array.Empty<GeminiPart>();

        string? textContent = null;
        var toolCallsList = new List<ToolCall>();

        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part.text))
            {
                textContent = part.text;
            }
            if (part.functionCall != null)
            {
                var fc = part.functionCall;
                var args = fc.args ?? new GeminiFunctionCallArguments(null, null, null, null, null, null, null);
                toolCallsList.Add(new ToolCall(
                    Guid.NewGuid().ToString(),
                    new ToolCallFunction(
                        fc.name,
                        new ToolCallFunctionArguments
                        {
                            Id = args.id,
                            Action = args.action,
                            Path = args.path,
                            NewPath = args.newPath,
                            Query = args.query,
                            Content = args.content,
                            LineNumber = args.lineNumber
                        }
                    )
                ));
            }
        }

        var message = new Message(
            Role.Assistant,
            null,
            textContent,
            null,
            toolCallsList.Count > 0 ? [.. toolCallsList] : null
        );

        return new LlmResponse(
            model.Name,
            DateTime.UtcNow,
            message
        );
    }

    public string CreateMessagesJson(Message[] messages)
    {
        var chatMessages = messages.Where(a => a.Role != Role.System).Select(a =>
        {
            var parts = new List<object>();

            if (a.ToolCalls != null && a.ToolCalls.Length > 0)
            {
                foreach (var call in a.ToolCalls)
                {
                    parts.Add(new
                    {
                        functionCall = new
                        {
                            name = call.Function.Name,
                            args = new
                            {
                                id = call.Function.Arguments.Id,
                                action = call.Function.Arguments.Action,
                                path = call.Function.Arguments.Path,
                                newPath = call.Function.Arguments.NewPath,
                                query = call.Function.Arguments.Query,
                                content = call.Function.Arguments.Content,
                                lineNumber = call.Function.Arguments.LineNumber
                            }
                        }
                    });
                }
            }
            else if (a.Role == Role.Tool)
            {
                parts.Add(new
                {
                    functionResponse = new
                    {
                        name = a.ToolCallId,
                        response = new
                        {
                            result = a.Content
                        }
                    }
                });
            }
            else
            {
                parts.Add(new
                {
                    text = a.Content ?? ""
                });
            }

            return new
            {
                role = a.Role switch
                {
                    Role.Assistant => "model",
                    _ => "user"
                },
                parts = parts.ToArray()
            };
        });

        return JsonSerializer.Serialize(chatMessages, DefaultJsonSerializerOptions.JsonSerializeOptionsIndented);
    }

    public string CreateToolsJson(Tool[] tools)
    {
        if (tools == null || tools.Length == 0)
            return "";

        var declarations = tools.Select(tool => $@"
    {{
      ""name"": ""{JsonEscape(tool.Name)}"",
      ""description"": ""{JsonEscape(tool.Desciption)}"",
      ""parameters"": {{
        ""type"": ""object"",
        ""properties"": {{{string.Join(",", tool.Parameters.Select(parameter => $@"
          ""{JsonEscape(parameter.Name)}"": {{
            ""type"": ""{parameter.Type}"",
            ""description"": ""{JsonEscape(parameter.Description)}""{(parameter.Enum == null ? "" : $@",
            ""enum"": [{string.Join(", ", parameter.Enum.Select(e => $@"""{JsonEscape(e)}"""))}]")}
          }}"))}
        }},
        ""required"": [{string.Join(", ", tool.Parameters.Where(p => p.Optional == false).Select(parameter => $@"""{JsonEscape(parameter.Name)}"""))}]
      }}
    }}");

        return $@"
  ""tools"": [
    {{
      ""function_declarations"": [
        {string.Join(",", declarations)}
      ]
    }}
  ],";
    }

    public string CreateRequestJson(Model model, LlmRequest apiCall, LlmOptions? options = null)
    {
        options ??= new LlmOptions();

        var systemMessage = apiCall.Messages.FirstOrDefault(a => a.Role == Role.System);
        string systemInstructionPart = "";
        if (systemMessage != null && !string.IsNullOrWhiteSpace(systemMessage.Content))
        {
            systemInstructionPart = $@",
  ""system_instruction"": {{
    ""parts"": [{{ ""text"": ""{JsonEscape(systemMessage.Content)}"" }}]
  }}";
        }

        var toolsPart = CreateToolsJson(apiCall.Tools);

        var configFields = new List<string>();

        if (options.Temperature.HasValue)
            configFields.Add($"\"temperature\": {options.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        if (options.NumPredict.HasValue)
            configFields.Add($"\"maxOutputTokens\": {options.NumPredict.Value}");

        if (options.Stop != null && options.Stop.Length > 0)
        {
            var stops = string.Join(", ", options.Stop.Select(s => $"\"{JsonEscape(s)}\""));
            configFields.Add($"\"stopSequences\": [{stops}]");
        }

        string generationConfigPart = "";
        if (configFields.Count > 0)
        {
            generationConfigPart = $@",
  ""generationConfig"": {{
    {string.Join(@",
    ", configFields)}
  }}";
        }

        return $@"{{
  ""contents"": {CreateMessagesJson(apiCall.Messages)}{systemInstructionPart},{toolsPart.TrimEnd(',')}{generationConfigPart}
}}";
    }

    private string? JsonEscape(string? value)
    {
        if (value == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '\"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.Append("\\u" + ((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}

// Gemini REST Model Definitions
internal record GeminiModelListResponse(
    GeminiModelRaw[]? models);

internal record GeminiModelRaw(
    string? name,
    string? version,
    string? displayName,
    string? description,
    int? inputTokenLimit,
    int? outputTokenLimit,
    string[]? supportedGenerationMethods);

internal record GeminiResponse(
    GeminiCandidate[]? candidates);

internal record GeminiCandidate(
    GeminiContent? content,
    string? finishReason,
    int? index);

internal record GeminiContent(
    GeminiPart[]? parts,
    string? role);

internal record GeminiPart(
    string? text,
    GeminiFunctionCall? functionCall);

internal record GeminiFunctionCall(
    string name,
    GeminiFunctionCallArguments? args);

internal record GeminiFunctionCallArguments(
    string? id,
    string? action,
    string? path,
    string? newPath,
    string? query,
    string? content,
    int? lineNumber);
