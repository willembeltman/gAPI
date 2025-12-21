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

            Code = $@"{GetNamespacesCode()}
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

#nullable enable

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
            ScopeIdentifier = Guid.NewGuid();
        }}

        private readonly NavigationManager NavigationManager;

        public event {StateChangedHandler.Name}? OnStateHasChanged;

        protected virtual HttpClient HttpClient {{ get; }}
        public virtual Guid ScopeIdentifier {{ get; }} 

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
        protected virtual async Task TryRefreshState()
        {{
            if (!LastChecked.HasValue || DateTime.UtcNow - LastChecked.Value > TimeSpan.FromMinutes(15))
            {{
                LastChecked = DateTime.UtcNow;

                await DecorateHttpClientAsync();
                var response = await HttpClient.GetAsync(""/Auth/GetState"");
                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadFromJsonAsync<{BaseResponseT.Name}<bool>>()
                    ?? throw new Exception(""Could not cast response data"");
                SetState(responseData.State);
            }}
        }}
        public virtual async Task<State?> GetState()
        {{
            await TryRefreshState();
            return State;
        }}
        public virtual async Task<bool> IsAuthenticated()
        {{
            var state = await GetState();
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

        protected virtual Task DecorateHttpClientAsync()
        {{
            string? scopeIdentifier = null;
            bool reload = false;
            if (HttpClient.DefaultRequestHeaders.TryGetValues(""ScopeIdentifier"", out var values))
            {{
                foreach (var value in values)
                {{
                    if (scopeIdentifier == null)
                    {{
                        scopeIdentifier = value;
                    }}
                    else
                    {{
                        reload = true;
                        break;
                    }}
                }}
            }}
            if (scopeIdentifier != ScopeIdentifier.ToString() || reload)
            {{
                while (HttpClient.DefaultRequestHeaders.Contains(""ScopeIdentifier""))
                {{
                    HttpClient.DefaultRequestHeaders.Remove(""ScopeIdentifier"");
                }}
                HttpClient.DefaultRequestHeaders.Add(""ScopeIdentifier"", ScopeIdentifier.ToString());
            }}
            return Task.CompletedTask;
        }}
        public virtual async Task<HttpResponseMessage> GetAsync(string url)
        {{
            await DecorateHttpClientAsync();
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return response;
        }}
        public virtual async Task<HttpResponseMessage> PostAsync(string url, MultipartFormDataContent content)
        {{
            await DecorateHttpClientAsync();
            var response = await HttpClient.PostAsync(url, content);
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
        public virtual async Task<HttpResponseMessage> PutAsync(string url, MultipartFormDataContent content)
        {{
            await DecorateHttpClientAsync();
            var response = await HttpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
            return response;
        }}
        public virtual async Task<HttpResponseMessage> DeleteAsync(string url)
        {{
            await DecorateHttpClientAsync();
            var response = await HttpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return response;
        }}
        public virtual Task AfterReceivedResponseIsParsedAsync(object objResponse)
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
