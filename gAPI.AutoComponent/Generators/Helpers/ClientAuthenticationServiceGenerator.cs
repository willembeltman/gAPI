using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Generators.Helpers;

public class ClientAuthenticationServiceGenerator : BaseGenerator
{
    public ClientAuthenticationServiceGenerator(
        ISharedReference state,
        ISharedReference iClientAuthenticationService,
        ISharedReference stateChangedHandler,
        string directory,
        string @namespace)
    {
        State = state;
        IClientAuthenticationService = iClientAuthenticationService;
        StateChangedHandler = stateChangedHandler;

        Directory = directory;
        Namespace = @namespace;

        Name = "ClientAuthenticationService";
        FileName = $"{Name}.g.cs";
    }

    public ISharedReference State { get; }
    public ISharedReference IClientAuthenticationService { get; }
    public ISharedReference StateChangedHandler { get; }

    public void GenerateCode()
    {
        //Code = "";
        //return;

        Reg(State);
        Reg(IClientAuthenticationService);
        Reg(StateChangedHandler);
        Reg("Microsoft.AspNetCore.Components.Authorization");
        Reg("System.Security.Claims");

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name} 
    : AuthenticationStateProvider, 
    {IClientAuthenticationService.Name}
{{
    public ClientAuthenticationService(
        IHttpClientFactory httpClientFactory)
    {{
        HttpClient = httpClientFactory.CreateClient(""WithCookies"");
        SessionId = Guid.NewGuid().ToString(""N"") + Guid.NewGuid().ToString(""N"");
    }}

    private readonly HttpClient HttpClient;
    private State? State;
    private DateTime? StateLastUpdate;

    public event {StateChangedHandler}? OnStateHasChanged;
    public string SessionId {{ get; private set; }} 

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct)
    {{
        var state = await GetStateAsync(ct);
        return state?.User != null;
    }}
    public async Task<{State}> GetStateAsync(CancellationToken ct)
    {{
        if (State == null ||
            StateLastUpdate == null ||
            DateTime.UtcNow - StateLastUpdate.Value > TimeSpan.FromMinutes(15))
        {{
            using var response = await GetAsync(""/__state"", ct);
            return State ?? throw new Exception(""Could not get state"");
        }}
        return State;
    }}
    public bool SetState({State} value)
    {{
        bool isDifferent = value.IsDifferent(State);

        State = value;
        StateLastUpdate = DateTime.UtcNow;

        if (isDifferent)
        {{
            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    GetAuthenticationState(State)));
            OnStateHasChanged?.Invoke();
        }}

        return isDifferent;
    }}

    public async Task<Stream> GetStreamAsync(string url, CancellationToken ct)
    {{
        if (!HttpClient.DefaultRequestHeaders.Contains(""accept""))
            HttpClient.DefaultRequestHeaders.Add(""accept"", ""text/event-stream"");
        DecorateHttpClientAsync();
        var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        TryUpdateState(response);
        var stream = response.Content.ReadAsStream();
        return stream;
    }}
    public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct)
    {{
        DecorateHttpClientAsync();
        var response = await HttpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        TryUpdateState(response);
        return response;
    }}
    public async Task<HttpResponseMessage> PostAsync(string url, MultipartFormDataContent content, CancellationToken ct)
    {{
        DecorateHttpClientAsync();
        var response = await HttpClient.PostAsync(url, content, ct);
        try
        {{
            response.EnsureSuccessStatusCode();
            TryUpdateState(response);
            return response;
        }}
        catch (Exception ex)
        {{
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error, ex);
        }}
    }}
    public async Task<HttpResponseMessage> PutAsync(string url, MultipartFormDataContent content, CancellationToken ct)
    {{
        DecorateHttpClientAsync();
        var response = await HttpClient.PutAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        TryUpdateState(response);
        return response;
    }}
    public async Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct)
    {{
        DecorateHttpClientAsync();
        var response = await HttpClient.DeleteAsync(url, ct);
        response.EnsureSuccessStatusCode();
        TryUpdateState(response);
        return response;
    }}

    private void DecorateHttpClientAsync()
    {{
        string? sessionId = null;
        bool reloadSessionId = false;
        if (HttpClient.DefaultRequestHeaders.TryGetValues(""X-SessionId"", out var sessionIdValues))
        {{
            foreach (var value in sessionIdValues)
            {{
                if (sessionId == null)
                {{
                    sessionId = value;
                }}
                else
                {{
                    reloadSessionId = true;
                    break;
                }}
            }}
        }}
        if (sessionId != SessionId.ToString() || reloadSessionId)
        {{
            while (HttpClient.DefaultRequestHeaders.Contains(""X-SessionId""))
            {{
                HttpClient.DefaultRequestHeaders.Remove(""X-SessionId"");
            }}
            HttpClient.DefaultRequestHeaders.Add(""X-SessionId"", SessionId.ToString());
        }}

        if (State != null)
        {{
            var StateData = State.CreateStateData();

            string? stateData = null;
            bool reloadStateData = false;
            if (HttpClient.DefaultRequestHeaders.TryGetValues(""X-StateData"", out var stateDataValues))
            {{
                foreach (var value in stateDataValues)
                {{
                    if (stateData == null)
                    {{
                        stateData = value;
                    }}
                    else
                    {{
                        reloadStateData = true;
                        break;
                    }}
                }}
            }}
            if (stateData != StateData.ToString() || reloadStateData)
            {{
                while (HttpClient.DefaultRequestHeaders.Contains(""X-StateData""))
                {{
                    HttpClient.DefaultRequestHeaders.Remove(""X-StateData"");
                }}
                HttpClient.DefaultRequestHeaders.Add(""X-StateData"", StateData[0]);
            }}
        }}
    }}
    private void TryUpdateState(HttpResponseMessage response)
    {{
        if (response.Headers.TryGetValues(""X-SessionId"", out var sessionIdValues))
        {{
            var sessionId = sessionIdValues?.FirstOrDefault();
            if (sessionId != null)
            {{
                SessionId = sessionId;
            }}
        }}
        if (response.Headers.TryGetValues(""X-StateData"", out var stateDataValues))
        {{
            var state = State.FromStateData(stateDataValues);
            if (state != null)
                SetState(state);
        }}
    }}

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {{
        try
        {{
            var state = await GetStateAsync(default);
            return GetAuthenticationState(state);
        }}
        catch (Exception ex)
        {{
            Console.WriteLine($""Auth check failed: {{ex.Message}}"");
        }}
        return CreateAnonymousAuthenticationState();
    }}
    private static AuthenticationState GetAuthenticationState(State state)
    {{
        if (state.User != null)
            return CreateAuthenticationStateFromState(state);
        return CreateAnonymousAuthenticationState();
    }}
    private static AuthenticationState CreateAnonymousAuthenticationState()
    {{
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymous);
    }}
    private static AuthenticationState CreateAuthenticationStateFromState(State state)
    {{
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, state.User!.Id.ToString()),
            new Claim(ClaimTypes.Name, state.User.Email),
            new Claim(""UserId"", state.User.Id.ToString())
        ];

        var identity = new ClaimsIdentity(claims, authenticationType: ""Cookie"");
        var user = new ClaimsPrincipal(identity);
        var authenticationState = new AuthenticationState(user);
        return authenticationState;
    }}
}}";
    }
}
