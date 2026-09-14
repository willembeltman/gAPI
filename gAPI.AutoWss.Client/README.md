# gAPI.AutoWss.Client

Automatic REST and SSE client generation for gAPI client applications.

## Introduction

`gAPI.AutoWss.Client` is the client-side part of gAPI's original API system.

AutoWss provides traditional client-to-server communication over HTTP using REST and JSON, while also providing server-to-client communication through Server-Sent Events (SSE).

The two communication directions use separate interfaces:

- `[GenerateApi]` defines client-to-server APIs.
- `[GenerateHub]` defines server-to-client APIs.

For client-to-server communication, gAPI generates a strongly typed client implementation for the `[GenerateApi]` interface.

For server-to-client communication, gAPI generates the client-side Hub infrastructure for the `[GenerateHub]` interface. The server accesses this Hub through `IClientContext`.

This keeps the two communication directions explicit while allowing both sides to use strongly typed C# interfaces.

___
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
        REST / JSON          SSE / JSON
___

### Client-to-server

A client-to-server API is defined using `[GenerateApi]`:

___
namespace Shared.Interfaces

[GenerateApi]
public interface IServerApi
{
    Task DoSomething(CancellationToken ct);
    Task<string> GetSomething(CancellationToken ct);
}
___

The client receives a generated strongly typed implementation:

___
@inject IServerApi api

await api.DoSomething(ct);
var result = await api.GetSomething(ct);
___

On the server, the same interface is implemented. The implementation is automatically wired into the generated API infrastructure:

___
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
___

No manually written REST client or controller endpoint is required for the generated API communication.

### Server-to-client

Server-to-client communication uses `[GenerateHub]`:

___
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
___

The client implements the Hub interface and registers the implementation with `IClientConnection`:

___
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
___

The server accesses the generated Hub through `IClientContext`:

___
public class ServerApi(
    IAuthenticationService authentication,
    IClientContext clientContext)
    : IServerApi
{
    public async Task DoSomething(CancellationToken ct)
    {
        var list = await clientContext.HostHub.ToAll.GetList(
            "test",
            GetFirst(),
            GetSecond(),
            ct);

        await foreach (var item in list)
        {
        }

        // ToAll invokes all connected clients.
        // For a one-to-one invocation, use ToSession with
        // the SessionId from IAuthenticationService.
    }

    public async Task<string> GetSomething(CancellationToken ct)
    {
        return "hello world";
    }
}
___

`IClientContext` provides access to the generated client Hubs and their routing targets.

`ToAll` targets all matching connected clients.

`ToSession` targets a specific authenticated session. The `SessionId` can be obtained through `IAuthenticationService`.

The underlying server-to-client transport is Server-Sent Events (SSE). For multiple API instances behind a load balancer, server-to-client communication can be routed through `gAPI.Fabric.Server`.

## How to Install

Install the required packages from NuGet:

___
dotnet add package gAPI.AutoWss.Client
dotnet add package gAPI.AutoAuth.Client
___

Register the generated API client and authentication infrastructure during application startup:

___
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAutoWssClient("https://127.0.0.1:7087");
builder.Services.AddAutoAuthClient("https://localhost:7087");

var app = builder.Build();
app.Run();
___

`AddAutoWssClient` configures the generated REST and SSE client infrastructure.

`AddAutoAuthClient` configures authenticated HTTP communication and authentication state handling.

## Communication Model

AutoWss uses asynchronous communication because the generated API performs I/O.

### Client → Server

`[GenerateApi]` supports:

- `Task`
- `Task<T>`

Methods can also accept `CancellationToken` parameters.

Client-to-server communication uses normal HTTP REST endpoints. The generated server endpoints are regular ASP.NET Core endpoints and can be exposed through OpenAPI and used with Swagger tooling like a traditional JSON API.

### Server → Client

`[GenerateHub]` supports:

- `Task`
- `IAsyncEnumerable<T>`

A server-to-client `Task<T>` is not supported because SSE is a one-way communication channel and does not provide a return channel for the invocation.

`IAsyncEnumerable<T>` can be used for server-to-client streaming.

## Serialization

REST and SSE API payloads use normal .NET JSON serialization.

API parameters and return values therefore follow the normal rules of the configured JSON serializer.

AutoWss does not use `AutoSerializer` for normal REST or SSE API payloads.

Authentication and session state are handled separately from normal API payload serialization.

## Authentication

Authentication uses normal HTTP cookies.

`gAPI.AutoAuth.Client` provides the authenticated HTTP communication and authentication state infrastructure used by AutoWss.Client.

Authentication and session information are handled through the normal HTTP communication infrastructure rather than being part of the API payload.

## Session and State

AutoWss uses the HTTP authentication and session infrastructure provided by AutoAuth.

The state parser can be customized through `IStateParser<AuthStateDto>` or through a custom type derived from `AuthStateDto`.

The state serialization can therefore be customized independently from the JSON serialization used for normal REST and SSE API payloads.

## Automatic Code Generation

`gAPI.AutoWss.Client` generates the client-side communication infrastructure from the shared interfaces.

For `[GenerateApi]`, gAPI generates the strongly typed client implementation used for client-to-server REST communication.

For `[GenerateHub]`, gAPI generates the client-side Hub infrastructure used for server-to-client SSE communication.

The generated infrastructure handles the communication details required by the interfaces, including:

- HTTP request generation
- JSON parameter serialization
- JSON response deserialization
- Authentication
- Session handling
- SSE connection handling
- Hub message dispatching
- Hub registration

Application code can therefore work directly with the generated strongly typed C# interfaces.

## Requirements

AutoWss is an asynchronous I/O system.

`[GenerateApi]` methods use `Task` or `Task<T>`.

`[GenerateHub]` methods use `Task` or `IAsyncEnumerable<T>` for server-to-client communication.

Synchronous API methods are not supported.

## Status

Beta