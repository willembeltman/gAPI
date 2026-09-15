<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

# gAPI.Core

Shared contracts, attributes, identifiers, DTOs, serializers, and runtime helpers used by the gAPI source generators and analyzers.

`gAPI.Core` contains the common building blocks required by the gAPI code-generation ecosystem. It is intentionally lightweight and primarily exists to provide a shared foundation for generated APIs, hubs, serializers, components, and related tooling.

## 🔧 What’s inside

### Attributes

The attributes used to describe and control code generation.

These include attributes for:

- API and Hub generation
- Minimal API generation
- Serializer generation
- Components and pages
- CRUD operations
- Authorization and authentication
- State management
- Storage files
- Entity relationships
- Property metadata and generation behavior

Examples include:

```
[GenerateApi]
[GenerateHub]
[GenerateSerializer]
[IsAuthorized]
[IsCreate]
[IsUpdate]
[IsReadOnly]
```

### DTOs

Shared data-transfer contracts used by generated code and gAPI runtime communication.

This includes request/response contracts, routing information, streaming messages, session state, subscriptions, synchronization, and cancellation messages.

### Identifiers

Strongly typed identifiers used throughout the generated communication infrastructure.

Examples include:

- `RequestId`
- `ServiceId`
- `ServiceMethodId`
- `SessionId`
- `StreamId`
- `UserId`
- `ClientConnectionId`
- `FabricConnectionId`

### Serializer infrastructure

Low-level serialization helpers and primitive serializers used by generated serializers and source generators.

This includes serializers for:

- Primitive types
- `Guid`
- `DateTime`
- `DateTimeOffset`
- `IFormFile`
- WebSocket message enums
- Application state

### Runtime helpers

Small runtime components required by generated code, including:

- Remote async enumerable support
- Remote async enumerators
- Cancellation and timeout handling
- Streaming helpers
- Remote exception handling
- Data-chunk stream readers
- Environment/path utilities

### Sse & Wss

Shared types and infrastructure used by generated Server-Sent Events and WebSocket communication.

This includes result types, SSE events, WebSocket converters, logging infrastructure, and related contracts.

## 🧠 Purpose

`gAPI.Core` is the common foundation shared by the gAPI source generators, analyzers, and generated code.

It does not provide a complete API framework by itself. Instead, it defines the contracts and supporting infrastructure that allow the individual gAPI packages to work together while keeping the actual code-generation logic in their respective analyzer/generator packages.

The architecture is intentionally split so that:

- `gAPI.Core` provides shared contracts and helpers
- gAPI analyzers inspect user code and attributes
- gAPI source generators produce the required implementation
- generated code uses the contracts and runtime helpers from `gAPI.Core`

This keeps the generated code strongly typed while avoiding a large runtime dependency.

## 📦 Part of the gAPI ecosystem

`gAPI.Core` is used by packages such as:

- `gAPI.AutoApi`
- `gAPI.AutoWss`
- `gAPI.AutoSerializer`
- `gAPI.AutoComponents`
- `gAPI.CodeGen.*`

Each package builds on the same core contracts and conventions.

## 🚀 Status

`gAPI.Core` is part of the ongoing development of the gAPI framework.

The API and internal contracts may still change while the framework is under active development.

— Willem-Jan Beltman