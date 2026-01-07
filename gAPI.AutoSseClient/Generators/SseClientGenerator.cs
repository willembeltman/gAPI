using System.Linq;

namespace gAPI.AutoSseClient.Generators
{
    internal class SseClientGenerator : BaseGenerator
    {
        internal SseClientGenerator(
            ServiceContext dataModel)
        {
            DataModel = dataModel;

            Directory = dataModel.Config.HubClients_Destination.Directory;
            Namespace = dataModel.Config.HubClients_Destination.Namespace;

            Name = "SseClient";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }

        public void GenerateCode()
        {
            Reg("Newtonsoft.Json");
            Reg("System.Net");
            Reg("System.Text");

            Reg(DataModel.IClientAuthenticationService);
            Reg(DataModel.ISseManagerBase);
            Reg(DataModel.SseServiceId);
            Reg(DataModel.SseHostId);

            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace}
{{
    public class {Name}(
        {DataModel.IClientAuthenticationService} clientAuthenticationService,
        {DataModel.ISseManagerBase} sseManager,
        {DataModel.SseServiceId} serviceId)
    {{
        private readonly CancellationTokenSource Cts = new();

        public {DataModel.SseServiceId} ServiceId {{ get; }} = serviceId;
        public {DataModel.SseHostId}? SseHostId {{ get; private set; }}

        public async Task ConnectAsync()
        {{
            while (!Cts.IsCancellationRequested)
            {{
                try
                {{
                    var url = $""/ssehost/connect/{{WebUtility.UrlEncode(ServiceId.Value)}}"";
                    using var stream = await clientAuthenticationService.GetStreamAsync(url, Cts.Token);
                    using var streamReader = new StreamReader(stream);

                    var buffer = new StringBuilder();
                    var chunk = new char[1024];

                    while (!Cts.IsCancellationRequested)
                    {{
                        var read = await streamReader.ReadAsync(chunk.AsMemory(0, chunk.Length), Cts.Token);
                        if (read <= 0) break;

                        buffer.Append(chunk, 0, read);

                        while (TryExtractFrame(buffer, out var frame))
                        {{
                            ParseFrame(frame);
                        }}
                    }}
                }}
                catch (Exception ex)
                {{
                    Console.WriteLine($""Error with SSE: {{ex}}"");
                }}
            }}
        }}
        private static bool TryExtractFrame(StringBuilder buffer, out string frame)
        {{
            for (int i = 0; i < buffer.Length - 1; i++)
            {{
                if (buffer[i] == '\n' && buffer[i + 1] == '\n')
                {{
                    frame = buffer.ToString(0, i);
                    buffer.Remove(0, i + 2);
                    return true;
                }}
            }}

            frame = null!;
            return false;
        }}
        private void ParseFrame(string frame)
        {{
            string? eventName = null;
            var dataBuilder = new StringBuilder();

            var lines = frame.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {{
                if (line.StartsWith(""event:""))
                {{
                    eventName = line[6..].TrimStart();
                }}
                else if (line.StartsWith(""data:""))
                {{
                    if (dataBuilder.Length > 0)
                        dataBuilder.Append('\n');

                    dataBuilder.Append(line[5..].TrimStart());
                }}
            }}

            if (eventName == null) return;
            var data = dataBuilder.ToString();
            HandleSseMessage(eventName, data);
        }}
        private void HandleSseMessage(string eventName, string data)
        {{
            if (eventName == ""SseHostId"")
            {{
                if (long.TryParse(data, out var id))
                    SseHostId = new SseHostId(id);
            }}
            else if (eventName == ""SseMessage"")
            {{
                var message = JsonConvert.DeserializeObject<SseMessage>(data);
                if (message != null)
                    _ = sseManager.MessageReceived(message);
            }}
        }}

        public async ValueTask DisposeAsync()
        {{
            Cts.Cancel();
            Cts.Dispose();
        }}
    }}
}}";
        }
    }
}