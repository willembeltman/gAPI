using gAPI.CodeGen.Backend.Config;
using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
using gAPI.CodeGen.Backend.Generators.Business.Models;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend;

public class BackendGenerator
{
    public BackendGenerator(BackendConfig config)
    {
        Config = config;

        DbContext = new DbContext(config.DbContextType);

        // Dtos/State
        StateDto = new StateDtoGenerator(
            this,
            config.DtoDirectory, config.DtoNamespace);

        // ResponseDtos/BaseResponse
        BaseResponse = new BaseResponseGenerator(
            this,
            config.ResponseDtoDirectory, config.ResponseDtoNamespace);

        // ResponseDtos/BaseResponse
        BaseResponseT = new BaseResponseTGenerator(
            this,
            config.ResponseDtoDirectory, config.ResponseDtoNamespace);

        BaseListResponseT = new BaseListResponseTGenerator(
            this,
            config.ResponseDtoDirectory, config.ResponseDtoNamespace);

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
                config.CrudHandlerInterfacesDirectory, config.CrudHandlerInterfacesNamespace,
                config.CrudHandlersDirectory, config.CrudHandlersNamespace,
                config.CustomMappingsDirectory, config.CustomMappingsNamespace,
                config.CrudServiceInterfacesDirectory, config.CrudServiceInterfacesNamespace,
                config.CrudServicesDirectory, config.CrudServicesNamespace))
            .ToArray();
    }

    public BackendConfig Config { get; }
    public DbContext DbContext { get; }
    public BaseResponseGenerator BaseResponse { get; }
    public BaseResponseTGenerator BaseResponseT { get; }
    public BaseListResponseTGenerator BaseListResponseT { get; }
    public StateDtoGenerator StateDto { get; }
    public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
    public AuthenticationStateGenerator AuthenticationState { get; }

    public DtoGenerator[] Dtos { get; private set; }

    public void Run()
    {
        // Dtos/State
        StateDto.GenerateCode();

        // ResponseDtos/BaseResponse
        BaseResponse.GenerateCode();

        // ResponseDtos/BaseResponseT
        BaseResponseT.GenerateCode();

        // ResponseDtos/BaseListResponseT
        BaseListResponseT.GenerateCode();

        // Interfaces/IServerAuthenticationService
        IServerAuthenticationService.GenerateCode();

        // Models/AuthenticationState
        AuthenticationState.GenerateCode();

        // Dtos/*
        foreach (var dto in Dtos) dto.GenerateCode();
    }
}
