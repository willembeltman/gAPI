# gAPI.AutoAuth.Server

Automatic authentication infrastructure for gAPI server applications.

# How It Works

gAPI.AutoAuth.Server provides the server-side authentication implementation used by gAPI.AutoApi.Server and gAPI.AutoWss.Server.

The analyzers communicate with authentication through IServerAuthenticationService, while AutoAuth takes care of the underlying authentication infrastructure, database configuration, authentication state, state serialization, synchronization, and dependency injection.

Add AutoAuth to the server during application startup:

```

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoAuthServer();

var app = builder.Build();
app.MapAutoAuthServer();

```

## How It Really Works

`gAPI.AutoApi.Server` and `gAPI.AutoWss.Server` use `IServerAuthenticationService` as their authentication connection point.

`gAPI.AutoAuth.Server` provides the implementation behind that interface.

```
AutoApi.Server / AutoWss.Server
                │
                ▼
IServerAuthenticationService
                │
                ▼
        AutoAuth.Server
```

Application code uses the generated `IAuthenticationService`.

```
IAuthenticationService
        │
        ├── State
        ├── SessionId
        └── Authentication
```

The authentication state is available directly through `State`.

## Authentication State

The default authentication state is `AuthStateDto`.

Applications can provide their own state by inheriting from it:

```
public class MyAuthState : AuthStateDto
{
    public string SomeValue { get; set; }
}
```

AutoAuth automatically uses the derived type as the authentication state for the generated `IAuthenticationService`.

The state is also serialized and synchronized as part of the gAPI authentication infrastructure.

## AuthUser

Application-specific users can be created by inheriting from `AuthUser`:

```
public class MyUser : AuthUser
{
    public string SomeProperty { get; set; }
}
```

AutoAuth automatically uses the derived user type throughout the authentication infrastructure.

## AuthenticationDbContext

The authentication database can be defined by inheriting from `AuthenticationDbContext<TUser>`:

```
public class MyAuthDbContext : AuthenticationDbContext<MyUser>
{
}
```

where `TUser` inherits from `AuthUser`.

AutoAuth automatically registers the database context with dependency injection.

The context can be injected directly or accessed through a factory.

## Database or Dummy Authentication

AutoAuth determines whether database-backed authentication is required from the application configuration.

When the following connection string exists:

```
ConnectionStrings:DefaultConnection
```

AutoAuth configures the authentication database using that connection string.

When no `DefaultConnection` is configured, AutoAuth creates a dummy authentication handler instead.

The dummy handler does not access a database and always has no authenticated user.

This allows the same authentication infrastructure to be used by applications that do not require database-backed authentication.

## Automatic Configuration

AutoAuth keeps the authentication configuration out of the application startup code.

It automatically connects:

- authentication services
- `AuthUser`
- `AuthenticationDbContext<TUser>`
- authentication state
- state serialization
- dependency injection
- database-backed or dummy authentication

The application only needs to provide the types it wants to customize.

## Application Usage

The generated `IAuthenticationService` can be injected directly into application services:

```
public class MyService
{
    private readonly IAuthenticationService _authentication;

    public MyService(IAuthenticationService authentication)
    {
        _authentication = authentication;
    }

    public async Task DoSomethingAsync()
    {
        var state = _authentication.State;

        // Use the authentication state...
    }
}
```

The exact state type depends on the `AuthStateDto` implementation used by the application.

## Optional Usage

AutoAuth is a convenience layer around the authentication interface used by the generated gAPI infrastructure.

If an application requires completely custom authentication behavior, the generated authentication implementation can be copied into the application and AutoAuth can then be removed.

This provides a simple default configuration without preventing full control when required.

## Status

This package is currently released as a beta version.