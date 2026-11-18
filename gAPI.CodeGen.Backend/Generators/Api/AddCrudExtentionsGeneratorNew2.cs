using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;
using gAPI.Core.Server.Interfaces;

namespace gAPI.CodeGen.Backend.Generators.Api;

public class AddCrudExtensionsGenerator : BaseGenerator
{
    public AddCrudExtensionsGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Extensions_Directory;
        Namespace = context.Config.Extensions_Namespace;

        Context = context;

        Name = "AddCrudExtensions";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference DbContext => Context.DbContext;
    public DtoGenerator[] Dtos => Context.Dtos;
    public SharedReference Mapping => Context.SharedReferences.Mapping;
    public SharedReference IUseCase => Context.SharedReferences.IUseCase;

    public void GenerateCode()
    {
        Reg(Mapping);
        Reg(IUseCase);
        Reg(DbContext);
        Reg("Microsoft.Extensions.DependencyInjection");

        var useCaseCode = "";
        var mappingCode = "";
        foreach (var dto in Dtos)
        {
            var entity = dto.Entity;
            var useCase = dto.CrudUseCase;
            var mapping = dto.CrudMapping;
            Reg(entity);
            Reg(useCase);
            Reg(mapping);
            useCaseCode += @$"
        services.AddScoped<{IUseCase}<{DbContext}, {entity}, {dto.FullName}, {entity.KeyProperty.TypeSimpleName}>, {useCase}>();";
            mappingCode += @$"
        services.AddScoped<{Mapping}<{DbContext}, {entity}, {dto.FullName}>, {mapping}>();";
        }

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public static class {Name}
{{
    public static IServiceCollection AddCrudUseCases(this IServiceCollection services)
    {{{useCaseCode}
        return services;
    }}

    public static IServiceCollection AddCrudMappings(this IServiceCollection services)
    {{{mappingCode}
        return services;
    }}
}}";
        Save(true);
    }
}
