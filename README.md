# gAPI – Full-stack Code Generation & App Scaffolding for .NET 10

**gAPI** is a powerful, end-to-end code generation platform for .NET 10, designed to turn your backend Service Model into a fully wired application — backend, frontend, storage, and real-time streaming included.

It combines **reflection, Roslyn analyzers, and source generators** to automate repetitive tasks, enforce consistency, and dramatically accelerate application development.

With gAPI, your **Service Model is the single source of truth**: define your services, DTOs, and contracts once, and gAPI generates a complete, type-safe, fully functional application stack.

---

## Core Features

- Automatic backend and frontend scaffolding  
- CRUD services, mappings, and handlers generated from DbContext  
- Real-time streaming with SSE or SignalR, both server and client sides  
- Fully generated Blazor WebAssembly, Blazor MAUI, and shared Razor projects  
- Built-in storage server for file and database management  
- Modular, pluggable architecture for flexibility and extensibility  

---

## Packages Overview

| Package | Purpose |
|---------|---------|
| **gAPI.AutoApi** | Server-side API generator. Automatically generates endpoints based on your Service Model. |
| **gAPI.AutoApiClient** | Client-side API generator. Generates type-safe API clients for your frontend. |
| **gAPI.AutoComponent** | Generates Blazor components automatically from your Service Model. |
| **gAPI.AutoCrud** | Generates CRUD services and mapping extensions for your entities. |
| **gAPI.AutoHub** | Generates SignalR infrastructure on the server side based on your interfaces. |
| **gAPI.AutoHubClient** | Generates SignalR client infrastructure for Blazor components based on interfaces. |
| **gAPI.AutoSse** | Generates server-side SSE (Server-Sent Events) infrastructure based on interfaces. |
| **gAPI.AutoSseClient** | Generates SSE client infrastructure for Blazor components based on interfaces. |
| **gAPI.CodeGen.Backend** | Backend generator: generates domain models, DTOs, services, CRUD handlers, and mappings. |
| **gAPI.CodeGen.Frontend** | Frontend generator: generates Blazor WASM, MAUI, and shared Razor projects from the backend. |
| **gAPI.ConfigTool** | Utility to manage and update appconfig.json references across projects. |
| **gAPI.Fabric** | Distributed event bus and messaging fabric for syncing data, streaming, and microservices communication. |
| **gAPI.Storage.Server** | File and database server that integrates directly with your projects for simple deployment. |

---

## Why gAPI?

1. **Productivity** – Generate complete backend and frontend layers in minutes.  
2. **Consistency** – Keep all your code type-safe and fully wired to your Service Model.  
3. **Real-time** – SSE and SignalR generators provide instant streaming without boilerplate.  
4. **Flexibility** – Modular packages let you adopt only what you need.  
5. **Scalability** – Designed to support complex enterprise-grade apps with minimal manual wiring.  

---

## License

MIT – free for commercial and personal use.
