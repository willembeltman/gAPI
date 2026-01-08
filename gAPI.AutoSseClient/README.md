# gAPI.AutoSseClient

**Automatic SSE/SignalR-like streaming for Blazor components.**

`gAPI.AutoSseClient` provides server-to-client streaming capabilities for Blazor applications
via a source generator and analyzer. No boilerplate, no manual wiring — just decorate your interfaces
and let the generator handle the rest.

---

## How it works

1. Create a contract interface in your shared contracts project:

CSharp

    using gAPI.Attributes;

    [GenerateHub]
    public interface ITestSseService
    {
        Task ServerToClientMethod(string? message);
    }

2. Implement the interface in your Blazor component:

CSharp

    @implements ITestSseService
    @implements IAsyncDisposable
    @inject ISseManager SseManager

    @code {
        async Task ITestSseService.ServerToClientMethod(string? message)
        {
            // Handle server messages
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync() 
            => await SseManager.SubscribeAsync(this);

        async ValueTask IAsyncDisposable.DisposeAsync() 
            => await SseManager.UnsubscribeAsync(this);
    }

3. Subscribe your component via SseManager during initialization and unsubscribe on dispose.

The source generator automatically wires up the server-to-client streaming, mimicking SignalR,
but fully integrated with gAPI conventions.

## Perfect for

- Blazor WASM projects
- Blazor MAUI projects
- Components that need real-time updates
- Teams wanting SignalR-like streaming without boilerplate

## Status
Version 0.0.1-alpha
Streaming generators are functional; actively maintained.

