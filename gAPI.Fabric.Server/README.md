<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

# gAPI.Fabric.Server

Internal distributed communication infrastructure for gAPI server applications.

`gAPI.Fabric.Server` provides the internal communication layer between multiple gAPI API/server hosts. It allows an API to send requests to other API hosts through Fabric, communicate with clients connected to those hosts, and coordinate responses across multiple targets.

## Usage

Create a small console application in your project and start the Fabric server:

Program.cs

```
using gAPI.Fabric.Server;

await FabricProgram.StartAsync();
```

Or

```
await gAPI.Fabric.Server.StartAsync();
```
(for short)

The Fabric server manages the connections between the different API hosts in your application.

## Monitoring

The runtime is independent from the commandline dashboard. When Fabric is hosted by an API, use `FabricServer.GetDashboardSnapshot()` to obtain an immutable, UI-neutral view of connections, services, throughput, sessions, and users. The returned `FabricDashboardSnapshot` can be serialized directly from an API endpoint or consumed by another frontend.

The commandline started through `FabricProgram.StartAsync()` uses that same snapshot, so the terminal and a future API frontend stay aligned without sharing presentation code.

## Communication Flow

A client normally connects to an API through gAPI.

When that API needs to communicate with clients, the request can be routed through Fabric to the API host or hosts where those clients are connected:

```
 API
  │
  ▼
Fabric
  │
  ▼
FabricHost
  │
  ▼
FabricManager
  │
  ▼
FabricHosts
  │
  ├──► APIs ──► Clients
  ├──► APIs ──► Clients
  └──► APIs ──► Clients
```

FabricManager performs the routing between the connected Fabric hosts. A request can therefore reach one or multiple API hosts depending on the routing and target information.

Responses can travel back through the same infrastructure.

For example, a client response can follow:

```
Clients
  │
  ▼
APIs
  │
  ▼
Fabric
  │
  ▼
FabricHost
  │
  ▼
FabricManager
  │
  ▼
FabricHosts
  │
  ▼
APIs
```

The receiving API can then continue processing the response.

## One-to-Many Communication

Fabric supports routing a request to multiple API hosts.

A single API request can therefore be distributed to multiple targets:

```
                    ┌──► API ──► Clients
                    │
API ──► Fabric ─────┼──► API ──► Clients
                    │
                    └──► API ──► Clients
```

Responses from multiple targets can be coordinated by Fabric and returned to the originating API.

This makes it possible to build distributed gAPI applications where clients and services can be connected to different API instances while still communicating as part of the same application.

## What Fabric Provides

### Internal API Communication

Fabric provides communication between separate gAPI API/server hosts.

### Request Routing

Requests can be routed through the Fabric network to the API hosts that can handle them based on registered services, users, sessions, and active connections.

The `FabricManager` coordinates this routing across the connected `FabricHosts`.

### One-to-Many Requests

A request can be sent to multiple API hosts simultaneously.

Fabric tracks the participating targets and coordinates their completion, cancellation, and exceptions.

### Response Coordination

Responses from multiple API hosts can be coordinated before the originating API continues processing.

### Client Communication

An API does not need to know which API host currently has a particular client connection.

Fabric can route the communication through the internal Fabric network to the appropriate API host or hosts.

### Streaming

Fabric also supports streaming communication.

Streaming routes are tracked independently so that data can continue flowing between the originating API, Fabric, the target API hosts, and the connected clients.

Cancellation, completion, and exceptions are propagated through the streaming route.

## Architecture

A typical deployment consists of multiple gAPI API hosts connected through Fabric:

```
                   Fabric
            ┌────────┴────────┐
            │                 │
        API Host 1        API Host 2
            │                 │
         Clients           Clients
```

Each API host runs its own gAPI server infrastructure.

The Fabric hosts provide the internal connections between those API instances and the `FabricManager` performs the routing between them.

```
          API
           │
           ▼
       FabricHost
           │
           ▼
      FabricManager
           │
           ├──► FabricHost ──► APIs
           │
           ├──► FabricHost ──► APIs
           │
           └──► FabricHost ──► APIs
```

Clients communicate with their API normally. Fabric is an internal server-side communication layer and is not exposed as a client-facing endpoint.

## Request Coordination

Fabric maintains state for active requests, including:

- originating API host
- target API hosts
- service
- user and session
- request state
- completed targets
- ready targets
- cancellation
- exceptions
- streaming routes

This allows distributed requests to be coordinated without requiring the originating API to know the physical location of every target.

## Part of the gAPI Ecosystem

- `gAPI.Core`  
  Shared contracts, identifiers, DTOs, attributes, serializers, and common infrastructure.

- `gAPI.Core.Server`  
  Server-side gAPI infrastructure such as HTTP, WebSocket, SSE, authentication, storage, and server services.

- `gAPI.Core.Client`  
  Client-side infrastructure for HTTP, WebSocket, SSE, navigation, and Blazor applications.

- `gAPI.Fabric.Server`  
  Internal communication and distributed routing between gAPI API/server hosts.

## Purpose

`gAPI.Fabric.Server` is intended for applications where multiple gAPI API instances need to operate as a distributed system while still being able to communicate with clients and services connected to other instances.

It provides the internal routing, request coordination, response handling, streaming, cancellation, and session-aware communication required to make those API instances work together.

## Status

This package is currently released as a beta version.
