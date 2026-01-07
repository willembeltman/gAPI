using gAPI.AutoComponent.Interfaces;

namespace gAPI.AutoComponent.Generators.Helpers
{
    internal class ClientAuthenticationServiceGenerator : BaseGenerator
    {
        public ClientAuthenticationServiceGenerator(
            ISharedReference baseResponse,
            ISharedReference baseResponseT,
            ISharedReference state,
            IClientAuthenticationServiceGenerator iClientAuthenticationService,
            StateChangedHandlerGenerator stateChangedHandler,
            string directory,
            string @namespace) : base(directory, @namespace)
        {
            BaseResponse = baseResponse;
            BaseResponseT = baseResponseT;
            State = state;
            IClientAuthenticationService = iClientAuthenticationService;
            StateChangedHandler = stateChangedHandler;

            Name = "ClientAuthenticationService";
            FileName = $"{Name}.g.cs";
        }

        public ISharedReference BaseResponse { get; }
        public ISharedReference BaseResponseT { get; }
        public ISharedReference State { get; }
        public IClientAuthenticationServiceGenerator IClientAuthenticationService { get; }
        public StateChangedHandlerGenerator StateChangedHandler { get; }

        public void GenerateCode()
        {
            Reg(BaseResponseT);
            Reg(BaseResponse);
            Reg(State);
            Reg(IClientAuthenticationService);
            Reg(StateChangedHandler);
            Reg("Microsoft.AspNetCore.Components");
            Reg("Microsoft.AspNetCore.Components.Authorization");
            Reg("System.Net.Http.Json");
            Reg("System.Security.Claims");

            Code = $@"{GetNamespacesCode()}#nullable enable

namespace {Namespace}
{{
    public class ClientAuthenticationService : AuthenticationStateProvider, {IClientAuthenticationService.Name}
    {{
        public ClientAuthenticationService(
            IHttpClientFactory httpClientFactory,
            NavigationManager navigationManager)
        {{
            NavigationManager = navigationManager;
            HttpClient = httpClientFactory.CreateClient(""WithCookies"");
            SessionId = Guid.NewGuid();
        }}

        private readonly NavigationManager NavigationManager;

        public event {StateChangedHandler.Name}? OnStateHasChanged;

        protected virtual HttpClient HttpClient {{ get; }}
        public virtual Guid SessionId {{ get; }} 

        protected virtual {State.Name}? State {{ get; set; }}
        protected virtual DateTime? LastChecked {{ get; set; }}

        protected virtual bool SetState({State.Name}? value)
        {{
            var res =
                State?.User?.Id != value?.User?.Id ||
                State?.CurrentCompany?.Id != value?.CurrentCompany?.Id ||
                State?.CurrentCompany?.Name != value?.CurrentCompany?.Name;

            State = value;

            if (res)
            {{
                OnStateHasChanged?.Invoke();
            }}

            return res;
        }}
        protected virtual async Task TryRefreshState(CancellationToken? token = null)
        {{
            token ??= new CancellationToken();
            if (!LastChecked.HasValue || DateTime.UtcNow - LastChecked.Value > TimeSpan.FromMinutes(15))
            {{
                LastChecked = DateTime.UtcNow;

                DecorateHttpClientAsync();
                var response = await HttpClient.GetAsync(""/Auth/GetState"", token.Value);
                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadFromJsonAsync<{BaseResponseT.Name}<bool>>()
                    ?? throw new Exception(""Could not cast response data"");
                SetState(responseData.State);
            }}
        }}
        public virtual async Task<State?> GetState(CancellationToken? token = null)
        {{
            await TryRefreshState(token);
            return State;
        }}
        public virtual async Task<bool> IsAuthenticated(CancellationToken? token = null)
        {{
            var state = await GetState(token);
            return state?.User != null;
        }}

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {{
            try
            {{
                await TryRefreshState();
                if (State?.User != null)
                {{
                    Claim[] claims =
                    [
                        new Claim(ClaimTypes.Name, State.User.Email),
                    new Claim(""UserId"", State.User.Id.ToString())
                    ];

                    var identity = new ClaimsIdentity(claims, authenticationType: ""Cookie"");
                    var user = new ClaimsPrincipal(identity);

                    return new AuthenticationState(user);
                }}
            }}
            catch (Exception ex)
            {{
                Console.Error.WriteLine($""Auth check failed: {{ex.Message}}"");
            }}

            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(anonymous);
        }}

        protected virtual void DecorateHttpClientAsync()
        {{
            if (!HttpClient.DefaultRequestHeaders.Contains(""accept""))
                HttpClient.DefaultRequestHeaders.Add(""accept"", ""text/event-stream"");

            string? sessionId = null;
            bool reload = false;
            if (HttpClient.DefaultRequestHeaders.TryGetValues(""SessionId"", out var values))
            {{
                foreach (var value in values)
                {{
                    if (sessionId == null)
                    {{
                        sessionId = value;
                    }}
                    else
                    {{
                        reload = true;
                        break;
                    }}
                }}
            }}
            if (sessionId != SessionId.ToString() || reload)
            {{
                while (HttpClient.DefaultRequestHeaders.Contains(""SessionId""))
                {{
                    HttpClient.DefaultRequestHeaders.Remove(""SessionId"");
                }}
                HttpClient.DefaultRequestHeaders.Add(""SessionId"", SessionId.ToString());
            }}
        }}


        public async Task<Stream> GetStreamAsync(string url, CancellationToken ct)
        {{
            DecorateHttpClientAsync();
            var response = await HttpClient.GetStreamAsync(url, ct);
            return response;
        }}
        public virtual async Task<HttpResponseMessage> GetAsync(string url, CancellationToken? token = null, HttpCompletionOption? option = null)
        {{
            token ??= new CancellationToken();
            DecorateHttpClientAsync();
            var response = 
                option == null
                ? await HttpClient.GetAsync(url, token.Value)
                : await HttpClient.GetAsync(url, option.Value, token.Value);
            response.EnsureSuccessStatusCode();
            return response;
        }}
        public virtual async Task<HttpResponseMessage> PostAsync(string url, MultipartFormDataContent content, CancellationToken? token = null)
        {{
            token ??= new CancellationToken();
            DecorateHttpClientAsync();
            var response = await HttpClient.PostAsync(url, content, token.Value);
            try
            {{
                response.EnsureSuccessStatusCode();
                return response;
            }}
            catch (Exception ex)
            {{
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error, ex);
            }}
        }}
        public virtual async Task<HttpResponseMessage> PutAsync(string url, MultipartFormDataContent content, CancellationToken? token = null)
        {{
            token = token ?? new CancellationToken();
            DecorateHttpClientAsync();
            var response = await HttpClient.PutAsync(url, content, token.Value);
            response.EnsureSuccessStatusCode();
            return response;
        }}
        public virtual async Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken? token = null)
        {{
            token ??= new CancellationToken();
            DecorateHttpClientAsync();
            var response = await HttpClient.DeleteAsync(url, token.Value);
            response.EnsureSuccessStatusCode();
            return response;
        }}
        public virtual Task AfterReceivedResponseIsParsedAsync(object objResponse, CancellationToken? token = null)
        {{
            if (objResponse is {BaseResponse.Name} responseData)
            {{
                if (SetState(responseData.State))
                {{
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                }}

                if (!string.IsNullOrWhiteSpace(responseData.RedirectPath))
                {{
                    NavigationManager.NavigateTo(responseData.RedirectPath);
                }}
            }}

            return Task.CompletedTask;
        }}
    }}
}}
";
        }
    }
}
