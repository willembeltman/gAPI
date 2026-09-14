# gAPI.AutoAuth.Client

Automatic authentication infrastructure for gAPI client applications.

`gAPI.AutoAuth.Client` provides the client-side authentication implementation used by `gAPI.AutoApi.Client` and `gAPI.AutoWss.Client`.

It implements the `IClientAuthenticatedHttpClient` interface and takes care of authenticated HTTP communication, authentication state, serialization, synchronization, and dependency injection.

## How It Works

`gAPI.AutoApi.Client` and `gAPI.AutoWss.Client` use `IClientAuthenticatedHttpClient` as their authentication connection point.

`gAPI.AutoAuth.Client` provides the implementation behind that interface.

___
AutoApi.Client / AutoWss.Client
              │
              ▼
IClientAuthenticatedHttpClient
              │
              ▼
       AutoAuth.Client
___

Application code uses the generated `IAuthenticatedHttpClient`.

___
IAuthenticatedHttpClient
        │
        ├── State
        ├── GetStateAsync()
        └── Authenticated HTTP
___

## Authentication State

The default authentication state is `AuthStateDto`.

Applications can provide their own state by inheriting from it:

___
public class MyAuthState : AuthStateDto
{
    public string SomeValue { get; set; }
}
___

AutoAuth automatically uses the derived type as the authentication state for the generated `IAuthenticatedHttpClient`.

The state is serialized and synchronized with the server as part of the gAPI authentication infrastructure.

## IAuthenticatedHttpClient

The generated `IAuthenticatedHttpClient` provides application code with access to the authenticated client and its current authentication state.

The state can be accessed directly:

___
var state = authenticatedHttpClient.State;
___

or retrieved asynchronously:

___
var state = await authenticatedHttpClient.GetStateAsync();
___

The returned state is an `AuthStateDto`, or the application's own derived state type.

## Automatic Configuration

AutoAuth configures the client-side authentication infrastructure automatically.

It connects:

- authenticated HTTP communication
- authentication state
- state serialization
- state updates
- dependency injection

The generated `AutoApi` and `AutoWss` client code can therefore use `IClientAuthenticatedHttpClient` without requiring the application to manually configure the authentication infrastructure.

## Authentication State Synchronization

Authentication state is part of the communication between the server and client.

AutoAuth handles the serialization and updates required to keep the state synchronized.

Application code can therefore work with the strongly typed authentication state instead of manually handling:

- authentication state serialization
- state updates
- state retrieval
- authenticated HTTP configuration

## Application Usage

The generated `IAuthenticatedHttpClient` can be injected directly into application services:

___
public class MyService
{
    private readonly IAuthenticatedHttpClient _authentication;

    public MyService(IAuthenticatedHttpClient authentication)
    {
        _authentication = authentication;
    }

    public async Task DoSomethingAsync()
    {
        var state = await _authentication.GetStateAsync();

        // Use the authentication state...
    }
}
___

When the current state is already available, the `State` property can be used directly.

## Optional Usage

AutoAuth is a convenience layer around the authentication interface used by the generated gAPI infrastructure.

If an application requires completely custom authentication behavior, the generated authentication implementation can be copied into the application and AutoAuth can then be removed.

This provides a simple default configuration without preventing full control when required.

## Status

This package is currently released as a beta version.