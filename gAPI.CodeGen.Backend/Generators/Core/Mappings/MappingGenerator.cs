using gAPI.CodeGen.Backend.Generators.Core.CrudHandlers;
using gAPI.CodeGen.Backend.Generators.Shared.Public.Dtos;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Core.CrudMappings;

public class MappingGenerator : BaseGenerator
{
    public MappingGenerator(
        BackendGenerator context,
        DtoGenerator dto)
    {
        Directory = context.Config.Core_CrudMappingsDirectory;
        Namespace = context.Config.Core_CrudMappingsNamespace;

        Context = context;
        Dto = dto;

        Name = $"{Entity.Name.ToMultiple()}Mapping";
        FileName = $"{Name}.cs";
    }
    public BackendGenerator Context { get; }
    public DtoGenerator Dto { get; }

    public SharedReference ApplyOrderByExtension => Context.SharedReferences.ApplyOrderByExtension;
    public SharedReference IStorageService => Context.SharedReferences.IStorageService;
    public UseCasesGenerator CrudUseCase => Dto.CrudUseCase;
    public DbSet DbSet => Dto.DbSet;
    public Entity Entity => Dto.Entity;


    public void GenerateCode()
    {
        var typeEntity = Entity;
        var typeDto = Dto;

        var context = new NameContext()
        {
            DtoNameProperties = Entity.Properties
                .Where(a =>
                    a.IsHidden == false &&
                    a.IsLijst == false &&
                    a.NavigationDbSet != null)
                .Select(a => new DtoNameProperty()
                {
                    NameProperty = a,
                    ForeignEntityNameProperties = a.NavigationDbSet!.Entity.Properties
                        .Where(a => a.IsName != null)
                        .ToArray()
                })
                .ToArray(),

            MatchedEntityProperties = Entity.Properties
                .Where(a =>
                    a.IsStateManaged == null &&
                    a.IsHidden == false &&
                    a.IsLijst == false &&
                    a.NavigationDbSet == null &&
                    a.IsReadOnly == false),

            MatchedDtoProperties = Entity.Properties
                .Where(a =>
                    a.IsHidden == false &&
                    a.IsLijst == false &&
                    a.NavigationDbSet == null)
        };

        Reg(ApplyOrderByExtension);
        if (Entity.IsStorageFileUrlProperty)
            Reg(IStorageService);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}(
    gAPI.Core.Interfaces.IUseCase<{typeEntity.FullName}, {typeDto.FullName}, {typeEntity.KeyProperty.TypeSimpleName}> useCase{(Entity.IsStorageFileUrlProperty ? @$", 
    {IStorageService} storageService" : "")}) 
    : gAPI.Core.Interfaces.Mapping<{typeEntity.FullName}, {typeDto.FullName}>
{{
    public override {typeEntity.FullName} ToEntity(
        {typeDto.FullName} dto, 
        {typeEntity.FullName} entity)
    {{{string.Join("",
context.MatchedEntityProperties
.Select(a => $@"
        entity.{a.Name} = dto.{a.Name};"))}

        return entity;
    }}

    public override async Task<{typeDto.FullName}> ToDtoAsync(
        {typeEntity.FullName} entity, 
        {typeDto.FullName} dto,
        CancellationToken ct)
    {{{string.Join("", context.MatchedDtoProperties.Select(p => $@"
        dto.{p.Name} = entity.{p.Name};"))}
        {string.Join("", context.DtoNameProperties.Select(a => $@"
        dto.{a.Name}Name = 
            {string.Join(" + \" \" + \r\n                ",
    a.ForeignEntityNameProperties!
        .Select(p => p.IsName?.Format($"(entity?.{a.NameProperty!.Name}?.{p.Name} ?? default)"))
)};
"))}
        await ExtendDto(dto, ct);

        return dto;
    }}

    public override IAsyncEnumerable<{typeDto.FullName}> ProjectToDtosAsync(
        IQueryable<{typeEntity.FullName}> entities,
        string[]? orderby, 
        int? skip, 
        int? take,
        CancellationToken ct)
    {{  
        var dtos = entities
            .Select(entity => new {typeDto.FullName}()
            {{{string.Join("",
context.MatchedDtoProperties
.Select(p => $@"
                {p.Name} = entity.{p.Name},"))}
#nullable disable{string.Join("",
context.DtoNameProperties
.Select(a => $@"
                {a.Name}Name = 
                    {string.Join(" + \" \" + \r\n                        ",
a.ForeignEntityNameProperties
    .Select(p => p.IsName.Format($"entity.{a.NameProperty!.Name}.{p.Name}"))
)},"))}
#nullable enable
            }})
            .{ApplyOrderByExtension}(orderby);

        if (skip != null)
        {{
            dtos = dtos.Skip(skip.Value);
        }}
        if (take != null)
        {{
            dtos = dtos.Take(take.Value);
        }}

        return EnumerateDtosAsync(dtos, ct);
    }}

    public override async Task ExtendDto(
        {typeDto.FullName} dto,
        CancellationToken ct)
    {{{(Entity.IsStorageFileUrlProperty ? @$"
        dto.StorageFileUrl = await storageService.GetStorageFileUrlAsync(dto.{Entity.KeyProperty.Name}.ToString(), ""{Entity.Name}"", ct);" : "")}
        dto.CanUpdate = await useCase.CanUpdateAsync(dto, ct);
        dto.CanDelete = await useCase.CanDeleteAsync(dto, ct);
    }}
}}";

        Save();
    }
}

#nullable disable
public class DtoNameProperty
{
    public EntityProperty NameProperty { get; set; }
    public EntityProperty[] ForeignEntityNameProperties { get; set; }
    public string Name => NameProperty.Name;
}

public class NameContext
{
    public IEnumerable<EntityProperty> MatchedEntityProperties { get; set; }
    public IEnumerable<EntityProperty> MatchedDtoProperties { get; set; }
    public DtoNameProperty[] DtoNameProperties { get; set; }
}