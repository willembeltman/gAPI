<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

# gAPI.AutoApi.Server

Automatic REST and SSE server generation for gAPI applications.

## Introduction

`gAPI.AutoApi.Server` is the server-side part of gAPI's original API system.

AutoApi provides traditional client-to-server communication over HTTP using REST and JSON, while also providing server-to-client communication through Server-Sent Events (SSE).

The two communication directions use separate interfaces:

- `[GenerateApi]` defines client-to-server APIs.
- `[GenerateHub]` defines server-to-client APIs.

For `[GenerateApi]`, gAPI generates the REST endpoints from the shared interface.

For `[GenerateHub]`, gAPI generates the server-side Hub infrastructure used to send messages to connected clients through `IClientContext`.

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

## Client-to-server

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

The server implements the same interface:

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

The implementation is automatically connected to the generated REST API infrastructure.

The client can then call the API through its generated strongly typed client:

```
@inject IServerApi api

await api.DoSomething(ct);

var result = await api.GetSomething(ct);
```

No manually written controller or REST endpoint is required.

## Server-to-client

Server-to-client communication uses `[GenerateHub]`:

```
namespace Shared.Interfaces

[GenerateHub]
public interface IClientHub
{
    Task DoSomething(CancellationToken ct);
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
dotnet add package gAPI.Core.Server
dotnet add package gAPI.AutoApi.Server
dotnet add package gAPI.AutoAuth.Server
```

Register AutoApi and authentication infrastructure during application startup:

```
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoApiServer();
builder.Services.AddAutoAuthServer();

var app = builder.Build();

app.MapAutoApiServer();
app.MapAutoAuthServer();

app.Run();
```

`AddAutoApiServer` registers the generated REST and SSE server infrastructure.

`MapAutoApiServer` maps the generated REST and SSE endpoints.

`AddAutoAuthServer` and `MapAutoAuthServer` configure the authentication and session infrastructure used by AutoApi.

## Configuration

AutoAuth can be configured using an `IConfiguration` instance passed to `MapAutoAuthServer`.

For example:

```

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoAuthServer();

var app = builder.Build();

app.MapAutoAuthServer(builder.Configuration);

```

The configuration can be provided through the application's `appsettings.json`.

The following settings are supported:

```

{
  "FrontendUrl": "https://localhost:1234",
  "UseMemoryDatabase": "false",
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVER;Database=DBNAME;Integrated Security=True;TrustServerCertificate=True",
    "FabricConnection": "Server=localhost;Port=9494;"
  }
}

```

`FrontendUrl` specifies the URL of the client application.

`UseMemoryDatabase` can be used to configure AutoAuth to use an in-memory database instead of the configured SQL Server database.

`ConnectionStrings:DefaultConnection` specifies the database connection used by the authentication database.

Other gAPI components can use additional connection strings, such as `StorageConnection` and `FabricConnection`. These are not required by AutoAuth itself, but can be provided through the same application configuration.

### Database Configuration

When `UseMemoryDatabase` is enabled, AutoAuth uses an in-memory database for authentication.

When it is disabled, AutoAuth uses `ConnectionStrings:DefaultConnection`.

If no `DefaultConnection` is configured, AutoAuth falls back to dummy authentication instead of requiring a database.

This makes it possible to use the same AutoAuth infrastructure during development, testing, and production without changing the application code.

## Communication Model

AutoApi uses standard HTTP REST for client-to-server communication and Server-Sent Events (SSE) for server-to-client communication.

### Client → Server

`[GenerateApi]` supports:

- `Task`
- `Task<T>`

Methods can also accept `CancellationToken` parameters.

The generated API is exposed as regular ASP.NET Core endpoints.

This means the generated REST API can be integrated with the normal ASP.NET Core middleware pipeline and can be exposed through OpenAPI and Swagger tooling.

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

`gAPI.AutoAuth.Server` provides the authentication and session infrastructure used by AutoApi.Server.

Authentication can therefore be integrated with the normal ASP.NET Core authentication and authorization middleware.

AutoApi follows the standard HTTP authentication model, making it straightforward to apply authorization policies and middleware to the generated API endpoints.

## Session and State

AutoApi uses the authentication and session state infrastructure provided by AutoAuth.

The normal API payloads continue to use JSON serialization.

State parsing can be customized through `IStateParser<AuthStateDto>` or through a custom type derived from `AuthStateDto`.

This state handling is separate from the JSON serialization used for REST and SSE API payloads.

## Multiple API Instances

AutoApi client-to-server communication uses normal HTTP requests and can therefore be distributed across multiple API instances behind a load balancer.

Server-to-client communication is different because an SSE connection belongs to a specific API instance.

When multiple API instances are used, `gAPI.Fabric.Server` can route server-to-client communication to the API instance holding the target client connection.

Without Fabric, server-to-client communication is handled directly by the API instance that owns the SSE connection.

## Performance

AutoApi uses standard HTTP REST requests for client-to-server communication.

Each client-to-server call is handled as a normal HTTP request and therefore carries the overhead associated with HTTP request processing, ASP.NET Core request scoping and request serialization.

Compared with AutoWss, AutoApi has higher communication overhead and higher latency.

Typical measured round-trip latency is approximately 10 ms for AutoApi compared with approximately 4 ms for AutoWss.

AutoApi is therefore intended for conventional REST/SSE communication where the standard HTTP model is more important than the lowest possible communication overhead.

## Automatic Code Generation

`gAPI.AutoApi.Server` generates the server-side communication infrastructure from the shared interfaces.

For `[GenerateApi]`, gAPI generates the REST API endpoints and infrastructure required to invoke the server implementation.

For `[GenerateHub]`, gAPI generates the server-side Hub infrastructure used for server-to-client SSE communication.

The generated infrastructure handles:

- REST endpoint generation
- HTTP request processing
- JSON serialization
- JSON deserialization
- Authentication
- Session handling
- SSE connection handling
- Hub message dispatching
- Client routing

Application code therefore works directly with strongly typed C# interfaces instead of manually maintaining REST endpoints and SSE message infrastructure.

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