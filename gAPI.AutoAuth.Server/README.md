<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

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
        ├── UserId
        └── Authentication
```

The authentication state is available directly through `State`.

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

## Authentication State

The default authentication state is `AuthStateDto`.

Applications can provide their own state by inheriting from it:

```
public class MyState : AuthStateDto
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

## Extending Authentication State

The authentication state can be extended by inheriting from `AuthStateDto`. When custom state serialization or comparison behavior is required, the `IStateParser<TState>` interface can also be implemented for the derived state type.

For example:

```

public class StateParser : IStateParser<MyState>
{
    public MyState? CreateCopy(MyState? value)
    {
        return value?.CreateCopy(); // Use gAPI.AutoSerializer
    }

    public bool IsDifferent(MyState? value1, MyState? value2)
    {
        if (value1 == null && value2 == null) return false;
        if (value1 == null || value2 == null) return true;

        return value1.IsDifferent(value2); // Use gAPI.AutoSerializer
    }

    public string? ToStringBase64(MyState? value)
    {
        if (value == null)
            value = new MyState();

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return Convert.ToBase64String(bytes);
    }

    public bool TryParse(string? value, out MyState state)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            state = new MyState();
            return false;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(value);
            var deserialized = JsonSerializer.Deserialize<MyState>(bytes);

            if (deserialized != null)
            {
                state = deserialized;
                return true;
            }
        }
        catch (Exception)
        {
        }

        state = new MyState();
        return false;
    }
}

```

The `CreateCopy` and `IsDifferent` implementations can use `gAPI.AutoSerializer` to provide automatic deep-copying and state comparison. Or you can of course implement the serialization and comparison logic yourself.

### Extending Authentication State Mapping

The server-side mapping between the authenticated user, token, IP information, and authentication state can also be extended by inheriting from `AuthenticationStateMapping<TUser, TState>`.

```

public class MyStateMapping
    : AuthenticationStateMapping<MyUser, MyState>
{
    public override async Task<MyState> ToDtoAsync(
        MyUser? dbUser,
        UserToken<MyUser>? dbToken,
        Ip<MyUser>? dbIp,
        MyState? receivedClientState,
        CancellationToken ct)
    {
        var state = await base.ToDtoAsync(
            dbUser,
            dbToken,
            dbIp,
            receivedClientState,
            ct);

        return state;
    }
}

```

By inheriting from the base mapping, an application can customize how authentication information is converted into its authentication state while retaining the default gAPI behavior through `base.ToDtoAsync(...)`.

This allows authentication behavior to be extended without replacing the complete AutoAuth infrastructure.

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