# gAPI – Full-stack Code Generation & App Scaffolding for .NET 10

**gAPI** is a powerful, end-to-end code generation platform for .NET 10, designed to turn your backend data model into a fully wired application — backend, frontend, storage, and real-time streaming included.

It combines **reflection, Roslyn analyzers, and source generators** to automate repetitive tasks, enforce consistency, and dramatically accelerate application development.

The platform is built around multiple layers of models and responsibilities:

- **Data Models / Entities** are used to generate the full backend business logic, including `Shared/Dtos`, `Shared/Interfaces`, `Core/Models`, `Core/Services` (base), `Core/CrudServices`, `Core/CrudMappings`, `Core/CrudHandlers`, and `Core/CrudMappingExtensions`.
- Developers are expected to **customize the CrudHandlers**, as these contain the core business logic of the application per table.
- Developers can also **add custom CRUD services or normal services by hand**. gAPI automatically generates the corresponding frontend components (`Index`, `Create`, `Details`, `Edit`, `Delete`) for crud services, which you can map to an external API for example. All services can be accessed from the frontend in a gRPC-like manner, and the `[IsPage]` attribute can be used on handwritten service methods to generate full pages automatically based on the method contract. 
- Together, all of these elements form the **Service Model**, which drives both the API communication and the frontend generation.
- On the frontend, developers have two options:
  - **CodeGen.Frontend** generates fully editable code directly into the Razor project.
  - **AutoComponents** generate the same code but by analyzer, so they update automatically whenever the Service Model changes. Developers can enable this by prefixing components with `Auto` (e.g., `<AutoCompanyForm ... />`), avoiding manual updates when the model evolves. 

By centralizing business logic, service contracts, and UI generation, gAPI allows developers to focus on application-specific features while automating repetitive scaffolding across the entire stack.

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
