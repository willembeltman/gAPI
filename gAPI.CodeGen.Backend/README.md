# gAPI.CodeGen.Backend

## Forge your entire backend from your DbContext.

gAPI.CodeGen.Backend is the primary backend generator of the gAPI platform.
It transforms your Entity Framework Core model into a fully structured backend domain — automatically.

Your database becomes your blueprint.

## What does it generate?

From your DbContext, this generator produces:

- Domain models
- Shared DTOs
- Response DTOs
- Core interfaces
- CRUD handlers
- CRUD services
- Mapping layers
- Core service contracts

Everything is created in structured project layers, ready to be consumed by the gAPI server and frontend generators.

## How it works

You reference this package in a small generator project and configure it:

    var root = EnvironmentPathHelper.GetRoot(Environment.ProcessPath!, "YourApp");
    var config = new gAPI.CodeGen.Backend.Config.BackendConfig(
        dbContextType: typeof(ApplicationDbContext),

        sharedDtoDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Shared\Dtos"),
        sharedDtoNamespace: "YourApp.Shared.Public.Dtos",
        sharedInterfacesDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Shared\Interfaces"),
        sharedInterfacesNamespace: "YourApp.Shared.Interfaces",
        sharedResponseDtoDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Shared\ResponseDtos"),
        sharedResponseDtoNamespace: "YourApp.Shared.ResponseDtos",

        coreInterfacesDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Core\Interfaces"),
        coreInterfacesNamespace: "YourApp.Core.Interfaces",
        coreModelsDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Core\Models"),
        coreModelsNamespace: "YourApp.Core.Models",
        coreCrudHandlersDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Core\CrudHandlers"),
        coreCrudHandlersNamespace: "YourApp.Core.CrudHandlers",
        coreCrudMappingsDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Core\CrudMappings"),
        coreCrudMappingsNamespace: "YourApp.Core.CrudMappings",
        coreCrudServicesDirectory: EnvironmentPathHelper.GetDirectory(root, @"YourApp.Core\CrudServices"),
        coreCrudServicesNamespace: "YourApp.Core.CrudServices");

    var generator = new gAPI.CodeGen.Backend.BackendGenerator(config);
    generator.Run();

The generator scans your EF Core model and writes all required domain, contract and CRUD layers to disk.

## Why this exists

Because hand-writing CRUD layers is:

- Slow
- Repetitive
- Error-prone
- And never stays in sync

gAPI turns your DbContext into the single source of truth and generates your entire backend architecture around it.

## Status

Version 0.0.1-alpha
Core generation pipelines are active and expanding.