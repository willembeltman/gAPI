using gAPI.CodeGen.Backend.Generators.Business.CrudHandlers;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Business.CrudMappings;

public class CrudMappingGenerator : BaseGenerator
{
    public CrudMappingGenerator(CrudHandlerGenerator handler, DirectoryInfo directory, string @namespace)
    {
        Handler = handler;
        Dto = handler.Dto;
        DbSet = Dto.DbSet;
        Entity = DbSet.Entity;

        Directory = directory;
        Namespace = @namespace;
        Name = $"{Entity.Name.ToMultiple()}Mapping";
        FileName = $"{Name}.cs";
    }

    public CrudHandlerGenerator Handler { get; }
    public DtoGenerator Dto { get; }
    public DbSet DbSet { get; }
    public Entity Entity { get; }

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
            MatchedDtoProperties = Entity.Properties
                .Where(a =>
                    a.IsHidden == false &&
                    a.IsLijst == false &&
                    a.NavigationDbSet == null &&
                    a.IsReadOnly == false)
        };
        //var context = new EntityToDtoModel(typeEntity, typeDto);

        Reg("gAPI.Helpers");
        Reg("gAPI.AutoMapper");
        if (Entity.IsStorageFile)
            Reg("gAPI.Storage");

        Code = $@"{GetNamespacesCode()}namespace {Namespace}
{{
    public class {Name}({(Entity.IsStorageFile ? @"
        IStorageService storageService" : "")}) 
        : CustomMapping<{typeEntity.FullName}, {typeDto.FullName}>
    {{
        public override async Task<{typeDto.FullName}> ToDtoAsync(
            {typeEntity.FullName} entity, 
            {typeDto.FullName} dto,
            MapperInstance<{typeEntity.FullName}, {typeDto.FullName}> defaultMapper, 
            ISecurityHandler<{Dto.FullName}>? handler)
        {{{string.Join("",
context.MatchedDtoProperties
    .Select(p => $@"
            dto.{p.Name} = entity.{p.Name};"))}
{string.Join("",
context.DtoNameProperties
    .Select(a => $@"
            dto.{a.Name}Name = 
                {string.Join(" + \" \" + \r\n                ",
        a.ForeignEntityNameProperties!
            .Select(p => p.IsName?.Format($"(entity?.{a.NameProperty!.Name}?.{p.Name} ?? default)"))
    )};
"))}
            await ExtendDto(dto, handler);

            return dto;
        }}

        public override Task<{typeEntity.FullName}> ToEntityAsync(
            {typeDto.FullName} dto, 
            {typeEntity.FullName} entity, 
            MapperInstance<{typeEntity.FullName}, {typeDto.FullName}> defaultMapper)
        {{{string.Join("",
context.MatchedDtoProperties
    .Select(a => $@"
            entity.{a.Name} = dto.{a.Name};"))}

            return Task.FromResult(entity);
        }}

        public override IAsyncEnumerable<{typeDto.FullName}> ProjectToDtosAsync(
            IQueryable<{typeEntity.FullName}> entities,
            string[]? orderby, 
            int? skip, 
            int? take, 
            MapperInstance<{typeEntity.FullName}, {typeDto.FullName}> defaultMapper,
            ISecurityHandler<{Dto.FullName}>? handler)
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
                .ApplyOrderBy(orderby);

            if (skip != null)
            {{
                dtos = dtos.Skip(skip.Value);
            }}
            if (take != null)
            {{
                dtos = dtos.Take(take.Value);
            }}

            return EnumerateDtosAsync(dtos, handler);
        }}

        protected override async Task ExtendDto(
            {typeDto.FullName} dto, 
            ISecurityHandler<{Dto.FullName}>? handler)
        {{{(Entity.IsStorageFile ? @$"
            dto.StorageFileUrl = await storageService.GetStorageFileUrlAsync(dto.{Entity.KeyProperty.Name}.ToString(), ""{Entity.Name}"");" : "")}
            dto.CanUpdate = handler == null ? false : await handler.CanUpdateAsync(dto);
            dto.CanDelete = handler == null ? false : await handler.CanDeleteAsync(dto);
        }}
    }}
}}";

        Save();
    }
}

#nullable disable
internal class DtoNameProperty
{
    public EntityProperty NameProperty { get; set; }
    public EntityProperty[] ForeignEntityNameProperties { get; set; }
    public string Name => NameProperty.Name;
}

internal class NameContext
{
    public IEnumerable<EntityProperty> MatchedDtoProperties { get; set; }
    public DtoNameProperty[] DtoNameProperties { get; internal set; }
}