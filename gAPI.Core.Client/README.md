<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

# gAPI.Core.Client

Client-side infrastructure, services, navigation, HTTP, WebSocket, SSE, and Blazor components used by the gAPI ecosystem.

`gAPI.Core.Client` provides the client-side building blocks required by gAPI applications. It builds on `gAPI.Core` and contains functionality specific to client-side execution, including Blazor WebAssembly, HTTP communication, WebSockets, Server-Sent Events, navigation, and client-side state handling.

## 🔧 What’s inside

### HTTP

Client-side HTTP infrastructure used by generated APIs and authenticated communication.

### Wss

WebSocket client infrastructure for communicating with generated gAPI hubs and services.

This includes:

- WebSocket client connections
- Message sending
- Service subscriptions
- Streaming communication

### Sse

Server-Sent Events client infrastructure, including SSE connections and subscription management.

### Navigation

Client-side navigation abstractions and implementations for environments where a standard Blazor navigation manager is not available.

### Razor components

Reusable Blazor components and helpers for common gAPI client functionality, including:

- Loading and error views
- Login and home redirects
- Form file handling
- Data sources for lists and forms
- Cookie handling

### Configuration & extensions

Client configuration and registration helpers used to integrate gAPI functionality into client applications.

## 🧠 Purpose

`gAPI.Core.Client` is the client-side foundation of the gAPI ecosystem.

It builds on `gAPI.Core`, which provides the shared contracts, attributes, DTOs, identifiers, serializers, and common helpers used throughout gAPI.

The architecture is split into three layers:

- `gAPI.Core` provides shared contracts and common infrastructure
- `gAPI.Core.Server` provides server-side runtime and infrastructure
- `gAPI.Core.Client` provides client-side runtime and infrastructure

This allows generated client code to share the same strongly typed contracts with the server while keeping client-specific dependencies separate.

## 📦 Part of the gAPI ecosystem

`gAPI.Core.Client` is used by the client-side components of gAPI, including generated API clients, hubs, components, and Blazor applications.

It depends on `gAPI.Core` for the shared contracts and types used throughout the ecosystem.

## 🚀 Status

`gAPI.Core.Client` is part of the ongoing development of the gAPI framework.

The API and internal contracts may still change while the framework is under active development.

— Willem-Jan Beltman