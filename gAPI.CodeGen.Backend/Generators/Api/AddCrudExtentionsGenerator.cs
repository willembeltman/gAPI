using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Models;

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

    public DtoGenerator[] Dtos => Context.Dtos;
    public SharedReference IUseCase => Context.SharedReferences.IUseCase;
    public SharedReference Mapping => Context.SharedReferences.Mapping;   

    public void GenerateCode()
    {
        Reg(IUseCase);
        Reg(Mapping);

        var useCaseCode = "";
        var mappingCode = "";
        foreach (var dto in Dtos)
        {
            var entity = dto.Entity;
            var useCase = dto.CrudUseCase;
            var mapping = dto.CrudMapping;
            var key = entity.KeyProperty.TypeSimpleName;
            Reg(useCase);
            Reg(mapping);
            useCaseCode += @$"
        services.AddScoped<{IUseCase}<{entity.FullName}, {dto.FullName}, {key}>, {useCase.Name}>();";
            mappingCode += @$"
        services.AddScoped<{Mapping}<{entity.FullName}, {dto.FullName}>, {mapping.Name}>();";
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
