# gAPI.AutoWss.Client

Automatic WebSocket client generation for gAPI client applications.

## Introduction

`gAPI.AutoWss.Client` is the client-side part of gAPI's WebSocket communication system.

AutoWss provides strongly typed client-to-server and server-to-client communication over WebSockets.

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

The client receives a generated strongly typed implementation:

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

`IClientConnection` is the client-side connection used to register and unregister Hub implementations.

`IClientContext` provides the server-side access to generated client Hubs.

`ToAll` targets all matching connected clients.

`ToSession` targets a specific authenticated session.

For server-to-client streaming, an `IAsyncEnumerable<T>` supplied to a `ToAll` invocation can be consumed independently by multiple clients. Applications should therefore take this into account when supplying enumerables for one-to-many communication.

For one-to-one communication, use `ToSession` with the appropriate `SessionId`.

## How to Install

Install the required packages from NuGet:

```
dotnet add package gAPI.Core.Client
dotnet add package gAPI.AutoWss.Client
dotnet add package gAPI.AutoAuth.Client
```

Register AutoWss and authentication infrastructure during application startup:

```
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAutoWssClient("https://localhost:7087");
builder.Services.AddAutoAuthClient("https://localhost:7087");

var app = builder.Build();
app.Run();
```

`AddAutoWssClient` configures the generated WebSocket client infrastructure.

`AddAutoAuthClient` configures authenticated HTTP communication and authentication state handling.

## Communication Model

AutoWss uses WebSockets for both communication directions.

### Client → Server

`[GenerateApi]` supports:

- `Task`
- `Task<T>`
- `IAsyncEnumerable<T>` response
- Multiple `IAsyncEnumerable<T>` parameters

Methods can also accept `CancellationToken` parameters.

Client-to-server communication is always one-to-one.

### Server → Client

`[GenerateHub]` supports:

- `Task`
- `IAsyncEnumerable<T>` response
- Multiple `IAsyncEnumerable<T>` parameters

`Task<T>` is not supported for server-to-client communication.

The server can target one or multiple clients through `IClientContext`.

For one-to-one communication, use `ToSession`.

When targeting multiple clients, an `IAsyncEnumerable<T>` supplied as an argument can be enumerated independently for each client.

## Streaming

AutoWss supports bidirectional streaming with `IAsyncEnumerable<T>`.

An `IAsyncEnumerable<T>` can be used as a response and as a method argument.

Multiple `IAsyncEnumerable<T>` arguments are supported.

`IAsyncEnumerable<T>` arguments must be top-level arguments. Nested `IAsyncEnumerable<T>` values are not supported.

Streaming uses backpressure between the producer and consumer.

AutoWss handles the streaming protocol without requiring the application to manually coordinate the order of streaming operations.

## Serialization

AutoWss uses `AutoSerializer` for communication payload serialization.

`AutoSerializer` is built into the AutoWss communication infrastructure and does not need to be referenced separately.

AutoWss uses a binary serialization format optimized for the generated communication protocol.

The maximum frame size is 10 MB, limiting a single call to a maximum payload of 10 MB.

## Authentication

Authentication and session state are handled through the AutoAuth infrastructure.

Authentication is established through HTTP cookies.

The WebSocket connection is associated with the authenticated session when the connection is established.

## Session and State

AutoWss associates WebSocket connections with authenticated HTTP sessions.

AutoAuth provides the HTTP state endpoint used to establish and maintain the session state.

A session is identified by a 256-bit `SessionId`.

A time-limited token associated with the session is used when establishing the WebSocket connection so the server can identify the session.

Session state can be managed locally by the API instance or through Fabric when multiple API instances are used.

## Fabric

`gAPI.Fabric.Server` can be used to route server-to-client communication between multiple AutoWss API instances behind a load balancer.

Fabric is required for server-to-client communication when the target client is connected to another API instance.

Client-to-server communication does not require Fabric for multi-instance deployments.

## Performance

AutoWss is designed around a lightweight WebSocket communication model.

Messages are constructed using deferred execution over `Span<byte>` rather than building large intermediate object graphs.

The protocol avoids the additional communication and abstraction layers required by frameworks such as SignalR.

The result is substantially lower communication overhead and higher throughput for generated API calls.

## Automatic Code Generation

`gAPI.AutoWss.Client` generates the client-side communication infrastructure from the shared interfaces.

For `[GenerateApi]`, gAPI generates the strongly typed client implementation used for client-to-server WebSocket communication.

For `[GenerateHub]`, gAPI generates the client-side Hub infrastructure used for server-to-client WebSocket communication.

The generated infrastructure handles:

- WebSocket communication
- Serialization
- Deserialization
- `IAsyncEnumerable<T>` streaming
- Streaming backpressure
- Cancellation
- Exceptions
- Hub registration
- Incoming Hub dispatching
- Session information

Application code can therefore work directly with strongly typed C# interfaces.

## Requirements

AutoWss is an asynchronous I/O system.

`[GenerateApi]` supports:

- `Task`
- `Task<T>`
- `IAsyncEnumerable<T>`

`[GenerateHub]` supports:

- `Task`
- `IAsyncEnumerable<T>`

`Task<T>` is not supported for `[GenerateHub]`.

`IAsyncEnumerable<T>` arguments must be top-level method arguments and cannot be nested.

Synchronous API methods are not supported.

## Status

Beta