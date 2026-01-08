# gAPI.CodeGen.Frontend

## Generate your entire Blazor frontend from your backend.

gAPI.CodeGen.Frontend is the primary frontend generator of the gAPI platform.
It transforms your backend Service Model into a complete, fully wired Blazor application — automatically.

- No scaffolding.
- No duplicated DTOs.
- No broken contracts.
- Your backend becomes the single source of truth.

## What does it generate?

From your backend service model ([GenerateApi] services and generated CRUD services), this package generates:

- Blazor WebAssembly Project
- Pages
- Components
- Navigation
- Client bindings
- API clients
- Blazor MAUI Project
- Native Blazor MAUI UI layer
- Mobile-ready layouts
- Shared client infrastructure
- Shared Razor Project
- Layouts
- Components
- Imports
- Base UI architecture
- Navigation structure
- Everything is wired automatically.

## How it works

gAPI.CodeGen.Frontend is a reflection-based and Roslyn analyzer powered generator.

You reference this package in your solution, then invoke the generator with:

Assembly references to your backend

Generator configuration (output paths, project layout, naming rules, etc.)

The generator scans your Service Model and produces ready-to-run Blazor projects.

Your backend defines:

- Which services exist
- Which CRUD endpoints exist
- Which DTOs exist
- Which UI pages and components must exist

The frontend is built directly from that blueprint.

## Why this exists

Because manually wiring frontends to APIs is:

- Slow
- Error-prone
- Repetitive
- And never stays in sync

gAPI flips the model:

Your backend is your UI contract.

Change your backend → regenerate → your UI updates automatically.

## Perfect for

- Rapid application prototyping
- Microservice-based systems
- Internal tools
- Serverless and small Azure App Service deployments
- Teams that want to move fast without frontend boilerplate hell

Status

Version 0.0.1-alpha
Active development – core generation pipelines are in place.