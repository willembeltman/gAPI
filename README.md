# gAPI

Core library for the gAPI framework – fast, type-safe API generation and data abstraction for .NET.

gAPI forms the foundation of the gAPI (Generated API Framework) ecosystem.
It provides the essential building blocks that power gAPI’s automatic API generation, serialization, and data-mapping systems.

## 🔧 What’s inside

This package contains the core components that are reused across the gAPI ecosystem:

### AutoComparer – Deep, type-aware object comparison utilities.
Supports custom equality logic and recursive comparison for complex models.

### AutoMapper – A lightweight, reflection-based mapping engine for DTO ↔ Entity conversion.
Includes type models, factories, and customizable mapping extensions.

### AutoSerializer – Unified serialization infrastructure for efficient, version-safe data exchange.

### EntityFrameworkDisk – A local, disk-based implementation of DbContext and DbSet for offline persistence.
Ideal for caching, testing, or lightweight storage scenarios.

### Storage – Abstractions for file-based and cloud storage, with implementations for

Mock (in-memory)

Azure Blob Storage

gAPI StorageServer

## 🧠 Purpose

gAPI is not a standalone API framework — it’s the common core used by:

gAPI.AutoApi – generated backend APIs

gAPI.AutoClient – generated client SDKs

gAPI.AutoComponents – generated component SDKs

gAPI.CodeGen.* – code generation tools

gAPI.StorageServer – storage and persistence services

Together, these packages allow developers to generate complete, type-safe, fully linked client–server systems with minimal boilerplate.

## 🚀 Status

This project is part of the early-stage development of gAPI.
The first pre-release version (v0.0.1) will become available soon.

Stay tuned for documentation and samples.

— Willem-Jan Beltman