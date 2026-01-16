using gAPI.CodeGen.Backend.Config;
using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
using gAPI.CodeGen.Backend.Generators.Business.Models;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend;

public class BackendGenerator
{
    public BackendGenerator(BackendConfig config)
    {
        Config = config;

        DbContext = new DbContext(config.DbContextType);

        BaseResponse = new SharedReference("gAPI.Dtos", "BaseResponse");
        BaseResponseT = new SharedReference("gAPI.Dtos", "BaseResponseT");
        BaseListResponseT = new SharedReference("gAPI.Dtos", "BaseListResponseT");

        // Dtos/State
        StateDto = new StateDtoGenerator(
            this,
            config.DtoDirectory, config.DtoNamespace);

        // Models/AuthenticationState
        AuthenticationState = new AuthenticationStateGenerator(
            this,
            config.BusinessModelsDirectory, config.BusinessModelsNamespace);

        // Interfaces/IServerAuthenticationService
        IServerAuthenticationService = new IServerAuthenticationServiceGenerator(this);

        // Generate DTOs
        Dtos = DbContext.DbSets
            .Where(a => !a.Entity.IsHidden)
            .Select(dbSet => new DtoGenerator(
                this, dbSet,
                config.DtoDirectory, config.DtoNamespace,
                config.CrudHandlersDirectory, config.CrudHandlersNamespace,
                config.CustomMappingsDirectory, config.CustomMappingsNamespace,
                config.CrudServiceInterfacesDirectory, config.CrudServiceInterfacesNamespace,
                config.CrudServicesDirectory, config.CrudServicesNamespace))
            .ToArray();
    }

    public BackendConfig Config { get; }
    public DbContext DbContext { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
    public StateDtoGenerator StateDto { get; }
    public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
    public AuthenticationStateGenerator AuthenticationState { get; }

    public DtoGenerator[] Dtos { get; private set; }

    public void Run()
    {
        // Dtos/State
        StateDto.GenerateCode();

        // Interfaces/IServerAuthenticationService
        IServerAuthenticationService.GenerateCode();

        // Models/AuthenticationState
        AuthenticationState.GenerateCode();

        // Dtos/*
        foreach (var dto in Dtos) dto.GenerateCode();
    }
}
