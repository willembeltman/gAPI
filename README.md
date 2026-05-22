# gAPI

> Experimental full-stack .NET ecosystem focused on code generation, strongly typed APIs, automatic UI generation, and real-time communication.

gAPI is a collection of libraries, Roslyn source generators, backend/frontend code generators, and infrastructure tools designed to drastically reduce boilerplate in modern .NET applications.

The project is heavily focused on:

* Strongly typed contracts
* Automatic API generation
* Real-time communication
* Generated CRUD flows
* Generated UI components/pages
* Minimal manual glue code
* Keeping full control when needed

The ecosystem is currently used in several of my own projects and serves as both an active portfolio project and an experimental application platform.

---

# Status

⚠️ **Important:** gAPI is currently **NOT production ready**.

This project is under active development and still changes frequently, including breaking changes.
The tooling works for my own workflows and projects, but installation/setup is not yet streamlined for external developers.

The goal right now is experimentation, architecture exploration, and building a cohesive ecosystem.

---

# Philosophy

A major design goal of gAPI is:

> Generate as much repetitive infrastructure as possible, while still allowing developers to fully take over any layer manually.

Most parts of the ecosystem are intentionally designed as **"drop-in" systems**:

* You can use the generated/default implementations
* Or completely replace them with your own

gAPI tries to avoid trapping developers in a rigid framework.

---

# Ecosystem Overview

## gAPI.Core

The shared core library used by the entire ecosystem.

Contains:

* Attributes
* Delegates
* Interfaces
* DDD-style key/id classes
* Helpers/utilities
* SSE/WSS helpers
* Serialization helpers

Every gAPI project references this library.

---

## gAPI.Core.Server

The server-side "drop-in" core package.

Contains:

* Authentication infrastructure
* Base data contexts
* Middleware
* Mapping helpers
* Account services
* Fabric client
* Storage client
* SSE/WSS communication helpers

---

## gAPI.Core.Client

The client-side "drop-in" core package.

Contains:

* Item/List datasource helpers
* Standard client views
* Authentication helpers
* `AuthenticatedHttpClient`
* HTTP client handlers

---

## gAPI.Core.ServiceBus

A lightweight abstraction layer to simplify service bus communication.

---

# Roslyn Source Generators

One of the core ideas behind gAPI is using Roslyn Source Generators to eliminate repetitive application plumbing.

---

## gAPI.AutoSerializer

Automatically generates serializers for classes marked with the `GenerateSerializer` attribute.

Can generate:

* Binary serializers
* Span serializers
* Multipart form data serializers
* Comparers
* Copy/create methods

Supported generation targets include:

* `BinaryReader/BinaryWriter`
* `Span<byte>`
* HTTP multipart content

---

## gAPI.AutoApi.Server

Generates server-side API endpoints for interfaces marked with:

* `GenerateApi`
* `GenerateMinimalApi`

Features:

* Automatic endpoint generation
* Automatic routing
* Service discovery/implementation wiring
* Authentication integration
* Minimal API support

Uses:

* `IServerAuthenticationService`

---

## gAPI.AutoApi.Client

Generates strongly typed client-side API proxies for interfaces marked with:

* `GenerateApi`
* `GenerateMinimalApi`

Features:

* Automatic HTTP proxy generation
* Strongly typed contracts
* Integrated authentication support

Uses:

* `IAuthenticatedHttpClient`

---

## gAPI.AutoSse.Server

The reverse communication counterpart to `AutoApi`.

Allows the **server to invoke client-side interfaces** marked with:

* `GenerateHub`

Targets can be:

* Services
* ViewModels
* Razor components

Uses SSE-based communication integrated with the gAPI authentication ecosystem.

---

## gAPI.AutoSse.Client

Client-side implementation for AutoSse.

Requires registration/subscription through:

* `IClientConnection`

---

## gAPI.AutoWss.Server / gAPI.AutoWss.Client

WebSocket-based counterparts of:

* `AutoApi`
* `AutoSse`

Because WebSockets are full-duplex, separate client/server communication models become unnecessary.

The architecture and usage remain mostly identical, meaning you can often switch from:

* HTTP + SSE
  to:
* Full WebSockets

by changing only a few startup lines in `Program.cs`.

---

# Automatic UI Generation

---

## gAPI.AutoComponent

Automatically generates reusable UI components for CRUD-style APIs.

When CRUD methods are marked with special attributes:

* `IsCreate`
* `IsRead`
* `IsUpdate`
* `IsDelete`
* `IsList`

the generator creates components such as:

* Details
* Dropdowns
* Forms
* Editable grids
* Lists
* Selectors
* Tables

---

## gAPI.AutoPage

Automatically generates pages from `GenerateApi` interfaces using:

* `IsPage` method attributes

Supports:

* Complex object graphs
* Arrays
* Immutable-style data structures

---

# Physical Code Generation

Unlike Roslyn source generators, these libraries generate actual source files into your project.

---

## gAPI.CodeGen.Backend

Generates a complete backend layer based on an Entity Framework `DbContext`.

Can generate:

* DTOs
* CRUD services
* Service interfaces
* State objects
* Mappers
* UseCases

UseCases act as an adapter layer between:

* Entity Framework
* CRUD services

and are intended to centralize:

* Authorization
* Record-level security
* Business access rules

---

## gAPI.CodeGen.Frontend

Generates frontend pages/components physically into your project.

Uses only the service model (`GenerateApi` interfaces) as source.

Can generate:

* Pages
* CRUD components
* Layout structures

You can:

* Keep the generated source
* Customize it manually
* Or switch to fully automatic runtime/generated components

---

# Infrastructure

---

## gAPI.Storage.Server

A lightweight storage server compatible with the gAPI ecosystem.

Comparable to:

* Azure Blob Storage

Supports:

* Local storage
* Azure Storage
* Custom implementations

Features:

* No SQL Server dependency
* Uses `gAPI.EntityFrameworkDisk`
* Can run on low-cost/free-tier hosting

---

## gAPI.Fabric.Server

A console application acting as a backplane for:

* SSE
* WSS communication

Responsible for:

* Routing messages between API nodes
* Coordinating real-time communication

Future plans include:

* Service discovery
* Service-to-service communication
* Full ServiceBus integration

---

# Tooling

---

## gAPI.ConfigTool

Small helper utility intended to configure projects when installing gAPI packages.

Currently not actively used.
The long-term goal is to reduce configuration complexity and rely more on environment variables.

---

## gAPI.Tester

Tiny internal test project.

It compiles.
That's about all the guarantees it currently provides 😄

Please don't use it as a reference. If you want to see example usage, check the `UwvLlm` project instead.

https://github.com/willembeltman/UwvLlm

---

## gAPI.EntityFrameworkDisk

An experimental file-based persistence library inspired by classic Entity Framework-style workflows.

Instead of using a traditional SQL database, data is stored directly on disk using generated serializers and indexed storage structures.

Features:

High-performance generated serializers
Key index files for fast lookups
Experimental lazy loading support
Experimental support for complex DDD-style entities and aggregates

The library is primarily designed for lightweight/self-hosted scenarios and is currently used internally by gAPI.Storage.Server.

It is still highly experimental and actively evolving.

---

# Current Weaknesses / TODO

## Authentication

The authentication system works, but the implementation still needs a cleaner architecture.

## Installation & Setup

Setup is currently not beginner-friendly.
A lot of internal assumptions still exist because the ecosystem was initially built for personal use.

## Generated Components

The generated UI/component system is still incomplete and evolving.

## Breaking Changes

Breaking changes happen frequently during development.

---

# Why Open Source?

The ecosystem has grown large enough that multiple active projects now depend on it, including publicly accessible ones.

Open sourcing it allows:

* Transparency
* Collaboration
* Portfolio visibility
* Architectural discussion
* Experimentation with advanced .NET tooling concepts

---

# Technologies & Concepts

gAPI heavily experiments with:

* Roslyn Source Generators
* ASP.NET Core
* Minimal APIs
* SSE
* WebSockets
* Strongly typed API contracts
* Code generation
* Immutable-style models
* Automated CRUD/UI generation
* Full-stack .NET workflows

---

# Final Note

This project is still evolving rapidly.

Some parts are surprisingly stable.
Some parts change daily.

The main purpose right now is building a cohesive developer ecosystem that minimizes repetitive work while keeping the flexibility of normal .NET development.
