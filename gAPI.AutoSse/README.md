# gAPI.AutoSse

**Automatic server-to-client streaming for gAPI backend services.**

`gAPI.AutoSse` is the backend companion to `gAPI.AutoSseClient`.  
It enables your services to stream messages directly to subscribed Blazor clients with zero boilerplate.

---

## How it works

1. Define a shared contract interface and decorate it with `[GenerateHub]`:

CSharp

    using gAPI.Attributes;

    [GenerateHub]
    public interface ITestSseService
    {
        Task ServerToClientMethod(string? message);
    }

2. Implement the interface in your backend service:

CSharp

    public class YourBackendService(
        IServerAuthenticationService auth,
        IClientContext ClientContext)
        : IYourBackendService
    {
        public async Task ClientToServerMethod(string? message)
        {
            await ClientContext
                .TestSseService
                .ToSession(auth.SessionId!.Value.ToString())
                .ServerToClientMethod(message);
        }
    }

The source generator/analyzer automatically wires up the service methods for SSE streaming.
Messages can be sent to a single session or broadcast to all subscribed clients.

## Perfect for
- Blazor WASM projects
- Blazor MAUI projects
- Real-time dashboards
- Internal tools requiring live updates
- Teams wanting SignalR-like streaming without writing boilerplate

## Status
Version 0.0.1-alpha
Streaming generators for backend services are functional and actively maintained.












