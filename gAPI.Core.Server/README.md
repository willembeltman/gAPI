# gAPI.Core.Server

Server-side infrastructure, services, authentication, storage, WebSocket, SSE, and Fabric components used by the gAPI ecosystem.

`gAPI.Core.Server` provides the server-side building blocks required to run gAPI applications. It builds on `gAPI.Core` and contains the runtime infrastructure that is specific to ASP.NET Core and server-side execution.

## 🔧 What’s inside

### Authentication

Server-side authentication and session infrastructure, including:

- Authentication middleware and handlers
- Account services
- Authentication state management
- User and token management
- Authentication security
- Authentication headers
- Database-backed and no-database authentication
- Authentication state mapping

### Fabric

Server-side components for communication between gAPI hosts and clients.

This includes:

- Fabric client communication
- Message conversion
- Client-to-host and host-to-client messaging
- Server-side request routing infrastructure

### Wss

WebSocket server infrastructure used by generated gAPI services and hubs.

This includes:

- Server WebSocket connections
- Connection senders
- Service subscriptions
- Server-side streaming communication

### Sse

Server-Sent Events infrastructure for maintaining server-side service subscriptions and communication.

### Storage

A common server-side storage abstraction with multiple implementations.

Supported implementations include:

- Mock / in-memory storage
- Azure Blob Storage
- gAPI StorageServer

The storage layer provides a common interface for storing, reading, updating, deleting, and streaming files without coupling applications to a specific storage provider.

### Entity Framework

Server-side persistence and authentication infrastructure based on Entity Framework Core.

This includes the authentication database model and entities used for:

- Users
- Sessions
- Tokens
- IP addresses
- Routes
- Authentication relationships

### Collections & caching

Server-side collections and caches used to manage:

- Server connections
- Service subscriptions
- Sessions
- Streaming state

### Configuration & extensions

Server configuration types and ASP.NET Core integration extensions for configuring and registering gAPI server functionality.

## 🧠 Purpose

`gAPI.Core.Server` is the server-side foundation of the gAPI ecosystem.

It contains functionality that requires ASP.NET Core, Entity Framework Core, server-side networking, or server-side infrastructure and therefore does not belong in the platform-independent `gAPI.Core` package.

The architecture is split into two layers:

- `gAPI.Core` provides shared contracts, attributes, DTOs, identifiers, serializers, and common helpers
- `gAPI.Core.Server` provides the server-side runtime and infrastructure built on top of those shared contracts

This separation keeps the common gAPI contracts lightweight while allowing server applications to opt into the infrastructure they require.

## 📦 Part of the gAPI ecosystem

`gAPI.Core.Server` is used by the server-side components of gAPI, including generated APIs, hubs, WebSocket communication, authentication, storage, and server-side service infrastructure.

It depends on `gAPI.Core` for the shared contracts and types used throughout the ecosystem.

## 🚀 Status

`gAPI.Core.Server` is part of the ongoing development of the gAPI framework.

The API and internal contracts may still change while the framework is under active development.

— Willem-Jan Beltman