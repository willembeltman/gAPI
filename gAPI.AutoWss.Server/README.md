# gAPI.AutoWss.Server

Automatic WebSocket API and Hub generation for gAPI server applications.

## Introduction

`gAPI.AutoWss.Server` is the server-side part of gAPI's WebSocket-based communication system.

AutoWss provides strongly typed client-to-server and server-to-client communication over WebSockets.

The two communication directions use separate interfaces:

- `[GenerateApi]` defines client-to-server APIs.
- `[GenerateHub]` defines server-to-client APIs.

For client-to-server communication, gAPI generates the server-side implementation infrastructure for the `[GenerateApi]` interface.

For server-to-client communication, gAPI generates the server-side Hub infrastructure for the `[GenerateHub]` interface. The server accesses this Hub through `IClientContext`.

AutoWss supports asynchronous streaming through `IAsyncEnumerable<T>` in both communication directions.

```
                 Shared Interfaces
                       │
              ┌────────┴────────┐
              │                 │
         [GenerateApi]     [GenerateHub]
              │                 │
              ▼                 ▼
        Client → Server    Server → Client
              │                 │
              ▼                 ▼
          WebSocket          WebSocket
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

    IAsyncEnumerable<string> GetList(
        string value,
        IAsyncEnumerable<string> first,
        IAsyncEnumerable<string> second,
        CancellationToken ct);
}
```

The client receives a generated strongly typed implementation and can call the API directly:

```
@inject IServerApi api

await api.DoSomething(ct);

var result = await api.GetSomething(ct);

var list = api.GetList(
    "test",
    GetFirst(),
    GetSecond(),
    ct);

await foreach (var item in list)
{
}
```

On the server, the same interface is implemented. The implementation is automatically wired into the generated WebSocket API infrastructure:

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

    public async IAsyncEnumerable<string> GetList(
        string value,
        IAsyncEnumerable<string> first,
        IAsyncEnumerable<string> second,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in first)
        {
        }

        await foreach (var item in second)
        {
        }

        yield return "1";
        yield return "2";
        yield return "3";
    }
}
```

No manually written WebSocket client, endpoint or message dispatching infrastructure is required for the generated API communication.

Client-to-server communication is always one-to-one.

Client-to-server communication does not require Fabric when multiple API instances are running behind a load balancer.

### Server-to-client

Server-to-client communication uses `[GenerateHub]`:

```
namespace Shared.Interfaces

[GenerateHub]
public interface IClientHub
{
    Task DoSomething(CancellationToken ct);

    IAsyncEnumerable<string> GetList(
        string value,
        IAsyncEnumerable<string> first,
        IAsyncEnumerable<string> second,
        CancellationToken ct);
}
```

On the client, the Hub interface is implemented and the instance is registered with `IClientConnection`:

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

    public async IAsyncEnumerable<string> GetList(
        string value,
        IAsyncEnumerable<string> first,
        IAsyncEnumerable<string> second,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in first)
        {
        }

        await foreach (var item in second)
        {
        }

        yield return "1";
        yield return "2";
        yield return "3";
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
        await clientContext.HostHub.ToAll.DoSomething(ct);
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

When an `IAsyncEnumerable<T>` is passed to a `ToAll` invocation, gAPI can asynchronously enumerate the supplied enumerable for multiple clients. Each client independently consumes the streamed data.

Applications using `ToAll` with streaming enumerables must therefore account for the enumerable potentially being consumed multiple times.

For one-to-one communication, use `ToSession` with the appropriate `SessionId`.

Server-to-client communication requires `gAPI.Fabric.Server` when multiple API instances are running behind a load balancer.

## How to Install

Install the required packages from NuGet:

```
dotnet add package gAPI.AutoWss.Server
dotnet add package gAPI.AutoAuth.Server
```

Register AutoWss and authentication infrastructure during application startup:

```
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoWssServer();
builder.Services.AddAutoAuthServer();

var app = builder.Build();

app.MapAutoWssServer();

app.Run();
```

`AddAutoWssServer` configures the generated WebSocket API and Hub infrastructure.

`AddAutoAuthServer` configures authentication and session state infrastructure.

`MapAutoWssServer` registers the required WebSocket endpoints.

## Communication Model

AutoWss uses asynchronous communication because the generated API performs I/O.

### Client → Server

`[GenerateApi]` supports:

- `Task`
- `Task<T>`
- `IAsyncEnumerable<T>`
- Multiple `IAsyncEnumerable<T>` parameters

`CancellationToken` parameters are supported.

Client-to-server communication is always one-to-one.

No Fabric backplane is required for client-to-server communication when multiple API instances are running behind a load balancer.

### Server → Client

`[GenerateHub]` supports:

- `Task`
- `IAsyncEnumerable<T>`
- Multiple `IAsyncEnumerable<T>` parameters

`Task<T>` is not supported for server-to-client communication.

The server-to-client direction can target multiple clients through `IClientContext`. Because WebSockets are used for bidirectional communication while the Hub invocation can be one-to-many, a `Task<T>` response cannot provide an unambiguous return destination.

For streaming operations sent to multiple clients, the same supplied enumerable may be asynchronously enumerated for each target client.

Use `ToSession` when the operation must be explicitly one-to-one.

## Streaming

AutoWss supports streaming with `IAsyncEnumerable<T>`.

An `IAsyncEnumerable<T>` can be used both as a return value and as a method parameter.

Multiple top-level `IAsyncEnumerable<T>` parameters are supported.

`IAsyncEnumerable<T>` parameters are supported as top-level arguments. Nested `IAsyncEnumerable<T>` values are not supported.

Streaming between the client and server uses backpressure, allowing the producer and consumer to coordinate the rate at which data is transferred.

For server-to-client calls targeting multiple clients, developers must account for the supplied enumerables potentially being enumerated asynchronously by multiple clients.

## Serialization

AutoWss uses `AutoSerializer` for communication payload serialization.

`AutoSerializer` is built into the AutoWss communication infrastructure and does not need to be referenced separately by applications using AutoWss.

AutoWss serialization follows the rules supported by `AutoSerializer`, including its support for `IAsyncEnumerable<T>` streaming.

## Authentication

Authentication and session state are handled through the AutoAuth infrastructure.

Authentication uses HTTP cookies for the authentication and session establishment process.

The WebSocket connection uses the established session information to associate the connection with the authenticated session.

## Session and State

AutoWss uses in-memory session state.

When Fabric is used, session state is managed through the Fabric infrastructure.

When Fabric is not used, session state is managed by the API/server instance.

AutoAuth provides the REST state endpoint used to establish and maintain the session state.

The session is identified by a 256-bit `SessionId`.

A time-limited token associated with the session is used when establishing the WebSocket connection so the server can identify the session.

## Fabric

Server-to-client communication can be routed through `gAPI.Fabric.Server`.

Fabric is required when multiple AutoWss API instances are deployed behind a load balancer and server-to-client communication must reach clients connected to another API instance.

Client-to-server communication does not require Fabric for this scenario because the client request is handled by the API instance to which the WebSocket connection is established.

## Automatic Code Generation

`gAPI.AutoWss.Server` generates the server-side communication infrastructure from the shared interfaces.

For `[GenerateApi]`, gAPI generates the server-side infrastructure that receives client-to-server WebSocket calls and dispatches them to the implementation of the shared interface.

For `[GenerateHub]`, gAPI generates the server-side Hub infrastructure used to invoke operations on connected clients through `IClientContext`.

The generated infrastructure handles the communication details required by the interfaces, including:

- WebSocket communication
- Parameter serialization
- Return value serialization
- `IAsyncEnumerable<T>` streaming
- Streaming backpressure
- Cancellation
- Hub routing
- Session targeting
- Client connection handling

Application code can therefore work directly with strongly typed C# interfaces.

## Requirements

AutoWss is an asynchronous I/O system.

`[GenerateApi]` methods use:

- `Task`
- `Task<T>`
- `IAsyncEnumerable<T>`

`[GenerateHub]` methods use:

- `Task`
- `IAsyncEnumerable<T>`

`Task<T>` is not supported for `[GenerateHub]`.

`IAsyncEnumerable<T>` parameters must be top-level method arguments and cannot be nested.

Synchronous API methods are not supported.

## Status

Beta