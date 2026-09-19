<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

# gAPI.AutoApi.Client

Automatic REST and SSE client generation for gAPI client applications.

## Introduction

`gAPI.AutoApi.Client` is the client-side part of gAPI's original API system.

AutoApi provides traditional client-to-server communication over HTTP using REST and JSON, while also providing server-to-client communication through Server-Sent Events (SSE).

The two communication directions use separate interfaces:

- `[GenerateApi]` defines client-to-server APIs.
- `[GenerateHub]` defines server-to-client APIs.

For client-to-server communication, gAPI generates a strongly typed client implementation for the `[GenerateApi]` interface.

For server-to-client communication, gAPI generates the client-side Hub infrastructure for the `[GenerateHub]` interface. The server accesses this Hub through `IClientContext`.

```
                 Shared Interfaces
                       │
              ┌────────┴────────┐
              │                 │
        [GenerateApi]     [GenerateHub]
              │                 │
              ▼                 ▼
       Client → Server   Server → Client
              │                 │
              ▼                 ▼
         REST / JSON        SSE / JSON
```

### Client-to-server

A client-to-server API is defined using `[GenerateApi]`:

```
namespace Shared.Interfaces

[GenerateApi]
public interface IServerApi
{
    Task DoSomething(CancellationToken ct);
    Task<string> GetSomething(CancellationToken ct);
}
```

The client receives a generated strongly typed implementation:

```
@inject IServerApi api

await api.DoSomething(ct);

var result = await api.GetSomething(ct);
```

On the server, the same interface is implemented. The implementation is automatically wired into the generated REST API infrastructure:

```
namespace Server.Services

public class ServerApi : IServerApi
{
    public async Task DoSomething(CancellationToken ct)
    {
        // Do something...
    }

    public async Task<string> GetSomething(CancellationToken ct)
    {
        return "hello world";
    }
}
```

No manually written REST client or controller endpoint is required for the generated API communication.

### Server-to-client

Server-to-client communication uses `[GenerateHub]`:

```
namespace Shared.Interfaces

[GenerateHub]
public interface IClientHub
{
    Task DoSomething(CancellationToken ct);
}
```

The client implements the Hub interface and registers the instance with `IClientConnection`:

```
namespace Client.Services

public class ClientHub(
    IClientConnection clientConnection)
    : IHostedService
    , IClientHub
{
    Task IHostedService.StartAsync(CancellationToken ct)
        => clientConnection.SubscribeAsync(this, ct);

    Task IHostedService.StopAsync(CancellationToken ct)
        => clientConnection.UnsubscribeAsync(this, ct);

    public async Task DoSomething(CancellationToken ct)
    {
        // Do something...
    }
}
```

The server accesses the generated Hub through `IClientContext`:

```
public class ServerApi(
    IAuthenticationService authentication,
    IClientContext clientContext)
    : IServerApi
{
    public async Task DoSomething(CancellationToken ct)
    {
        await clientContext.ClientHub.ToAll.DoSomething(ct);
    }

    public async Task<string> GetSomething(CancellationToken ct)
    {
        return "hello world";
    }
}
```

`IClientContext` provides access to the generated client Hubs and their routing targets.

`ToAll` targets all matching connected clients.

`ToSession` targets a specific authenticated session. The `SessionId` can be obtained through `IAuthenticationService`.

The underlying transport is Server-Sent Events (SSE).

For multiple API instances behind a load balancer, server-to-client communication can be routed through `gAPI.Fabric.Server`.

## How to Install

Install the required packages from NuGet:

```
dotnet add package gAPI.Core.Client
dotnet add package gAPI.AutoApi.Client
dotnet add package gAPI.AutoAuth.Client
```

Register AutoApi and authentication infrastructure during application startup:

```
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAutoApiClient("https://localhost:7087");
builder.Services.AddAutoAuthClient("https://localhost:7087");

var app = builder.Build();
app.Run();
```

`AddAutoApiClient` configures the generated REST and SSE client infrastructure.

`AddAutoAuthClient` configures authenticated HTTP communication and authentication state handling.

## Communication Model

AutoApi uses standard HTTP REST for client-to-server communication and Server-Sent Events (SSE) for server-to-client communication.

### Client → Server

`[GenerateApi]` supports:

- `Task`
- `Task<T>`

Methods can also accept `CancellationToken` parameters.

Client-to-server communication uses normal HTTP REST endpoints.

The generated server endpoints are regular ASP.NET Core endpoints and can be exposed through OpenAPI and used with Swagger tooling like a traditional JSON API.

### Server → Client

`[GenerateHub]` supports:

- `Task`

`Task<T>` is not supported for server-to-client communication because SSE is a one-way communication channel and does not provide a return channel for the invocation.

Server-to-client communication can target one or multiple clients through `IClientContext`.

For one-to-one communication, use `ToSession` with the appropriate `SessionId`.

## Serialization

AutoApi uses normal .NET JSON serialization for REST and SSE payloads.

API parameters and return values therefore follow the normal rules of the configured JSON serializer.

AutoApi does not use `AutoSerializer` for normal API payloads.

Authentication and session state are handled separately from normal API payload serialization.

## Authentication

Authentication uses normal HTTP cookies.

`gAPI.AutoAuth.Client` provides the authenticated HTTP communication and authentication state infrastructure used by AutoApi.Client.

Authentication is handled through the normal HTTP authentication mechanisms and can therefore be integrated with standard ASP.NET Core authentication middleware on the server.

## Session and State

AutoApi uses the authentication and session state infrastructure provided by AutoAuth.

The normal API payloads continue to use JSON serialization.

State parsing can be customized through `IStateParser<AuthStateDto>` or through a custom type derived from `AuthStateDto`.

This state handling is separate from the JSON serialization used for REST and SSE API payloads.

## Performance

AutoApi uses standard HTTP REST requests for client-to-server communication.

Each client-to-server call is handled as a normal HTTP request and therefore carries the overhead associated with HTTP request processing, ASP.NET Core request scoping and request serialization.

Compared with AutoWss, AutoApi has higher communication overhead and higher latency.

Typical measured round-trip latency is approximately 10 ms for AutoApi compared with approximately 4 ms for AutoWss.

AutoApi is therefore intended for conventional REST/SSE communication where the simplicity and standard HTTP model are more important than the lowest possible communication overhead.

## Automatic Code Generation

`gAPI.AutoApi.Client` generates the client-side communication infrastructure from the shared interfaces.

For `[GenerateApi]`, gAPI generates the strongly typed client implementation used for client-to-server REST communication.

For `[GenerateHub]`, gAPI generates the client-side Hub infrastructure used for server-to-client SSE communication.

The generated infrastructure handles:

- HTTP request generation
- JSON serialization
- JSON deserialization
- Authentication
- Session handling
- SSE connection handling
- Hub message dispatching
- Hub registration

Application code can therefore work directly with strongly typed C# interfaces.

## Requirements

AutoApi is an asynchronous I/O system.

`[GenerateApi]` supports:

- `Task`
- `Task<T>`

`[GenerateHub]` supports:

- `Task`

`Task<T>` is not supported for `[GenerateHub]`.

`IAsyncEnumerable<T>` is not supported by AutoApi.

Synchronous API methods are not supported.

## Status

Beta

# Example code

### Shared
``` CSharp
	[GenerateApi]
	public interface ITestApi
	{
		Task TestCallAsync(string text, CancellationToken ct);
	}

	[GenerateHub]
	public interface ITestHub
	{
		Task TestCallAsync(string text, CancellationToken ct);
	}

	public class StateDto : AuthStateDto
	{
		public int TestNumber { get; set; }
	}
```

### Backend
``` CSharp
	public class TestApi(
		IAuthenticationService authenticationService,
		ITestHubContext testHub, // Option 1
		IClientContext clientContext, // Option 2
		ILoggerFactory loggerFactory)
		: ITestApi
	{
		readonly ILogger Logger = loggerFactory.CreateLogger<TestApi>();

		public async Task TestCallAsync(string text, CancellationToken ct)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
				Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + " SendLocationAsync({location})", "hoi2");

			authenticationService.State.TestNumber++;
			await testHub.ToAll.TestCallAsync("Hello world", ct); // Option 1
			await clientContext.TestHub.ToAll.TestCallAsync("Hello world", ct); // Option 2
		}
	}
```

### Frontend
``` Razor
	@page "/Test"
	@implements IAsyncDisposable
	@implements ITestHub // Inherits from Shared interface
	@inject ITestApi server
	@inject IClientConnection connection
	@inject IAuthenticatedHttpClient authentication

	<PageTitle>Test</PageTitle>

	<button @onclick="DoTestCall">Send heen en terug</button>

	<h1>@(Wait1.ToString("F1"))ms heen</h1>
	<h1>@(Wait2.ToString("F1"))ms heen en terug</h1>
	<h3>@(TestNumber) hop counter</h3>

	@code {
		CancellationTokenSource Cts = new();
		CancellationToken Ct => Cts.Token;
		Stopwatch stopwatch = new();
		double Wait1;
		double Wait2;
		int TestNumber;

		protected override async Task OnInitializedAsync()
		{
			var authorized = await authentication.IsAuthenticatedAsync(Ct);
			if (authorized == false) { }
			await connection.SubscribeAsync(this, Ct);
		}

		async Task DoTestCall()
		{
			var state = await authentication.GetStateAsync(false, Ct);
			state.TestNumber++;
			TestNumber = state.TestNumber;
			StateHasChanged();

			stopwatch.Restart();
			await server.TestCallAsync("Hello world", Ct);
			Wait1 = stopwatch.Elapsed.TotalMilliseconds;

			state = await authentication.GetStateAsync();
			TestNumber = state.TestNumber;
			StateHasChanged();
		}

		public async Task TestCallAsync(string text, CancellationToken ct)
		{
			Wait2 = stopwatch.Elapsed.TotalMilliseconds;
		}

		public async ValueTask DisposeAsync()
		{
			await connection.UnsubscribeAsync(this);
			await Cts.CancelAsync();
			Cts.Dispose();
		}
	}
```