using gAPI.Delegates;
using gAPI.Dtos;
using gAPI.Ids;
using gAPI.Interfaces;
using gAPI.Sse;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using gAPI.Core.Extentions;
using gAPI.Extensions;

namespace gAPI.Core.Client;

public class AuthenticatedHttpClient<TStateDto>(
    IStateParser<TStateDto> stateSerializer,
    IHttpClientFactory httpClientFactory,
    NavigationManager navigation)
    : AuthenticationStateProvider,
      IAuthenticatedHttpClient<TStateDto>
    where TStateDto : AuthStateDto, new()
{
    private readonly HttpClient HttpClient =
        httpClientFactory.CreateClient("WithCookies");

    private readonly SemaphoreSlim StateLock = new(1, 1);

    private TStateDto? OldState { get; set; }
    private TStateDto? State;
    private DateTime StateLastUpdate;

    public event StateChangedHandler? OnStateHasChanged;

    public SessionId SessionId { get; private set; } = SessionId.New();
    public UserId UserId => new(State?.User?.Id.ToString());
    public Uri? BaseUri => HttpClient.BaseAddress;

    public bool ForceReconnect
    {
        get => State?.ForceReconnect == true;
        set => State?.ForceReconnect = value;
    }

    private Task<TStateDto>? StateFetchTask;
    private async Task<TStateDto> FetchStateAsync(CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, "/__state");
            var response = await HttpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            await TryUpdateStateAsync(response, ct);

            return State ?? throw new InvalidOperationException("State fetch failed");
        }
        finally
        {
            await StateLock.WaitAsync(ct);
            StateFetchTask = null;
            StateLock.Release();
        }
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct)
    {
        var state = await GetStateAsync(false, ct);
        return state.User != null;
    }
    public async Task<TStateDto> GetStateAsync(bool force = false, CancellationToken ct = default)
    {
        await StateLock.WaitAsync(ct);
        try
        {
            if (force == false &&
                State != null &&
                DateTime.UtcNow - StateLastUpdate <= TimeSpan.FromMinutes(15))
            {
                return State;
            }

            StateFetchTask ??= FetchStateAsync(ct);
        }
        finally
        {
            StateLock.Release();
        }

        return await StateFetchTask;
    }
    public async Task<string> GetStateDataAsync(bool force = false, CancellationToken ct = default)
    {
        var state = await GetStateAsync(force, ct);
        return stateSerializer.ToStringValuesBase64(state).ToString();
    }
    public async Task TryUpdateStateAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await StateLock.WaitAsync(ct);
        try
        {
            if (response.Headers.TryGetValues("X-SessionId", out var sessionValues) &&
                SessionId.TryParse(sessionValues, out var sessionId))
            {
                SessionId = sessionId;
            }

            if (response.Headers.TryGetValues("X-StateData", out var stateValues) &&
                stateSerializer.TryParse(stateValues, out var newState))
            {
                await UpdateStateInternal(newState);
            }
        }
        finally
        {
            StateLock.Release();
        }
    }
    public async Task TryUpdateStateAsync(ApiResult result, CancellationToken ct)
    {
        await StateLock.WaitAsync(ct);
        try
        {
            if (SessionId.TryParse(result.SessionData, out var sessionId))
                SessionId = sessionId;

            if (stateSerializer.TryParse(result.StateData, out var state))
                await UpdateStateInternal(state);
        }
        finally
        {
            StateLock.Release();
        }
    }
    public async Task TryUpdateStateAsync(string? stateData, CancellationToken ct)
    {
        if (stateData == null) return;
        await StateLock.WaitAsync(ct);
        try
        {
            if (stateSerializer.TryParse(stateData, out var state))
                await UpdateStateInternal(state);
        }
        finally
        {
            StateLock.Release();
        }
    }
    private async Task UpdateStateInternal(TStateDto value)
    {
        bool isDifferent = stateSerializer.IsDifferent(value, State);

        State = value;
        OldState = stateSerializer.CreateCopy(value);
        StateLastUpdate = DateTime.UtcNow;

        if (!isDifferent)
            return;

        NotifyAuthenticationStateChanged(
            Task.FromResult(GetAuthenticationState(value)));

        OnStateHasChanged?.Invoke();
    }
    public bool IsStateChanged()
    {
        if (stateSerializer.IsDifferent(OldState, State) == true)
        {
            OldState = stateSerializer.CreateCopy(State);
            return true;
        }
        return false;
    }

    public async Task<Stream> GetStreamAsync(string url, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("text/event-stream");

        var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        response.EnsureSuccessStatusCode();
        await TryUpdateStateAsync(response, ct);

        return await response.Content.ReadAsStreamAsync(ct);
    }
    public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Get, url);
        var response = await HttpClient.SendAsync(request, ct);

        response.EnsureSuccessStatusCode();
        await TryUpdateStateAsync(response, ct);

        return response;
    }
    public async Task<HttpResponseMessage> PostAsync(string url, MultipartFormDataContent content, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, url);
        request.Content = content;

        var response = await HttpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(error);
        }

        await TryUpdateStateAsync(response, ct);
        return response;
    }
    public async Task<HttpResponseMessage> PutAsync(string url, MultipartFormDataContent content, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Put, url);
        request.Content = content;

        var response = await HttpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        await TryUpdateStateAsync(response, ct);
        return response;
    }
    public async Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Delete, url);
        var response = await HttpClient.SendAsync(request, ct);

        response.EnsureSuccessStatusCode();
        await TryUpdateStateAsync(response, ct);

        return response;
    }
    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var pathAndQuery = "/" + navigation.ToBaseRelativePath(navigation.Uri);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Uri", pathAndQuery);
        request.Headers.TryAddWithoutValidation("X-SessionId", SessionId.ToString());
        request.Headers.TryAddWithoutValidation("X-StateData", stateSerializer.ToStringBase64(State));
        return request;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var state = await GetStateAsync(default);
            return GetAuthenticationState(state);
        }
        catch
        {
            return CreateAnonymousAuthenticationState();
        }
    }
    private static AuthenticationState GetAuthenticationState(TStateDto state)
    {
        return state.User != null
            ? CreateAuthenticationStateFromState(state)
            : CreateAnonymousAuthenticationState();
    }
    private static AuthenticationState CreateAnonymousAuthenticationState()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymous);
    }
    private static AuthenticationState CreateAuthenticationStateFromState(TStateDto state)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, state.User!.Id.ToString()),
            new Claim(ClaimTypes.Name, state.User.Email),
            new Claim("UserId", state.User.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Cookie");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void Dispose()
    {
    }
}
