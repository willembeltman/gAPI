using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Generators.Shared.Interfaces;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Core.CrudServices;

public class CrudServiceGenerator : BaseGenerator
{
    public CrudServiceGenerator(
        BackendGenerator context,
        DtoGenerator dto)
    {
        Directory = context.Config.Core_CrudServicesDirectory;
        Namespace = context.Config.Core_CrudServicesNamespace;

        Context = context;
        Dto = dto;

        Name = $"{Entity.Name!.ToMultiple()}Service";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DtoGenerator Dto { get; }

    public CrudServiceInterfaceGenerator ServiceInterface => Dto.ServiceInterface;

    public Entity Entity => Dto.Entity;
    public SharedReference BaseListResponseT => Context.SharedReferences.BaseListResponseT;
    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;

    public SharedReference IUseCase => Context.SharedReferences.IUseCase;
    public SharedReference Mapping => Context.SharedReferences.Mapping;
    public SharedReference IServerAuthenticationService => Context.IServerAuthenticationService;

    public void GenerateCode()
    {
        Reg(Dto);
        Reg(ServiceInterface);
        Reg(BaseResponseT);
        Reg("Microsoft.EntityFrameworkCore");
        if (Entity.IsStorageFileUrlProperty)
        {
            Reg("gAPI.Storage");
            Reg("Microsoft.AspNetCore.Http");
        }
        if (Entity.Properties
                .Any(p => p.IsStateManaged != null))
        {
            Reg(IServerAuthenticationService);

        }

        var loadBysCode = string.Join("", Entity.Properties
            .Where(a => a.IsNavigationItem)
            .Select(property =>
            {
                var nav = property.NavigationItemProperty!;
                var navent = nav.ParentEntity;
                var code = $@"

    public async Task<{BaseListResponseT.Name}<{Dto.Name}>> ListBy{nav.Name}({nav.TypeSimpleName} {nav.Name.ToCamelCase()}, int? skip, int? take, string[]? orderby, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        if (!await useCase.CanListAsync(ct))
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        var entities = useCase
            .ListAll()
            .Where(a => a.{nav.Name} == {nav.Name.ToCamelCase()});

        orderby = orderby == null || orderby.Length == 0 ? [""{Entity.KeyProperty.Name}""] : orderby;
        var dtos = mapping.ProjectToDtosAsync(entities, orderby, skip, take, ct);

        if (dtos == null)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorGettingData = true }};

        return new {BaseListResponseT.Name}<{Dto.Name}>()
        {{
            Success = true,
            Skip = skip ?? 0,
            Take = take ?? 0,
            CanCreate = await useCase.CanCreateAsync(ct),
            Response = dtos
        }};
    }}

    public async Task<{BaseListResponseT.Name}<{Dto.Name}>> ListNotBy{nav.Name}({nav.TypeSimpleName} {nav.Name.ToCamelCase()}, int? skip, int? take, string[]? orderby, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        if (!await useCase.CanListAsync(ct))
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        var entities = useCase
            .ListAll()
            .Where(a => a.{nav.Name} != {nav.Name.ToCamelCase()});

        orderby = orderby == null || orderby.Length == 0 ? [""{Entity.KeyProperty.Name}""] : orderby;
        var dtos = mapping.ProjectToDtosAsync(entities, orderby, skip, take, ct);

        if (dtos == null)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorGettingData = true }};

        return new {BaseListResponseT.Name}<{Dto.Name}>()
        {{
            Success = true,
            Skip = skip ?? 0,
            Take = take ?? 0,
            CanCreate = await useCase.CanCreateAsync(ct),
            Response = dtos
        }};
    }}";
                return code;
            }));

        string StateManagedProperties =
            string.Join("", Entity.Properties
                .Where(p => p.IsStateManaged != null)
                .Select(p =>
                {
                    if (p.IsStateManaged!.CheckForNull)
                        return $@"

        if (authenticationService.State.{p.IsStateManaged.Name} != null)
            dto.{p.Name} = authenticationService.State.{p.IsStateManaged.Name}{(p.IsStateManaged.UseValue ? p.IsStateManaged.IsString ? "!" : ".Value" : "")}; // From state";
                    else
                        return $@"

        dto.{p.Name} = authenticationService.State.{p.IsStateManaged.Name}{(p.IsStateManaged.UseValue ? p.IsStateManaged.IsString ? "!" : ".Value" : "")}; // From state";
                }));

        string ImmutableProperties =
            string.Join("", Entity.Properties
                .Where(p => p.IsImmutable)
                .Select(p =>
                {
                    return $@"

        dto.{p.Name} = entity.{p.Name}; // Immutable";
                }));

        var fileCode = "";
        if (Entity.IsStorageFileUrlProperty)
        {
            fileCode += $@"

    public async Task<{BaseResponseT.Name}<{Dto.Name}>> FileUpdate({Entity.KeyProperty.TypeSimpleName} {Dto.Name!.ToLower()}{Entity.KeyProperty.Name}, IFormFile? file, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};
        {(Entity.IsUser ? "" : string.Join("", StateManagedProperties))}
        var entity = await useCase.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name}, ct);
        if (entity == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorItemNotFound = true }};

        var dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        if (!await useCase.CanUpdateAsync(dto, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        if (file != null)
        {{
            using var storageFileStream = file.OpenReadStream();
            await storageService.SaveStorageFileAsync(entity, file.FileName, file.ContentType, storageFileStream, ct);
        }}

        dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        if (!await useCase.UpdateAsync(entity, dto, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorUpdatingState = true }};

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true,
            Response = dto
        }};
    }}

    public async Task<{BaseResponseT.Name}<bool>> FileDelete({Entity.KeyProperty.TypeSimpleName} {Dto.Name.ToLower()}{Entity.KeyProperty.Name}, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseResponseT.Name}<bool>() {{ ErrorNotAuthorized = true }};

        var entity = await useCase.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name}, ct);

        if (entity == null)
            return new {BaseResponseT.Name}<bool>() {{ ErrorItemNotFound = true }};

        var dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        if (!await useCase.CanDeleteAsync(dto, ct))
            return new {BaseResponseT.Name}<bool>() {{ ErrorNotAuthorized = true }};

        await storageService.DeleteStorageFileAsync(entity, ct);

        dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        if (!await useCase.UpdateAsync(entity, dto, ct))
            return new {BaseResponseT.Name}<bool>() {{ ErrorUpdatingState = true }};

        return new {BaseResponseT.Name}<bool>() 
        {{ 
            Success = true,
            Response = true 
        }};
    }}";
        }

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}(
    {IUseCase.FullName}<{Entity.FullName}, {Dto.Name}, {Entity.KeyProperty.TypeSimpleName}> useCase,
    {Mapping.FullName}<{Entity.FullName}, {Dto.Name}> mapping{(Entity.IsStorageFileUrlProperty ? @",
    IStorageService storageService" : "")}{(Entity.Properties.Any(a => a.IsStateManaged != null) ? $@",
    IServerAuthenticationService authenticationService" : "")})
    : {ServiceInterface.Name}
{{
    public async Task<{BaseResponseT.Name}<{Dto.Name}>> Create({Dto.Name} dto, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};{StateManagedProperties}

        var entity = await useCase.FindByMatchAsync(dto, ct);

        if (entity != null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorAlreadyUsed = true }};

        entity = mapping.ToEntity(dto, new {Entity.FullName}());

        if (!await useCase.CanCreateAsync(dto, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        if (!await useCase.AddAsync(entity, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorAttachingState = true }};

        dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true,
            Response = dto
        }};
    }}

    public async Task<{BaseResponseT.Name}<{Dto.Name}>> Read({Entity.KeyProperty.TypeSimpleName} {Dto.Name!.ToLower()}{Entity.KeyProperty.Name}, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        var entity = await useCase.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name}, ct);
        if (entity == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorItemNotFound = true }};

        var dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        if (!await useCase.CanReadAsync(dto, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true,
            Response = dto
        }};
    }}

    public async Task<{BaseResponseT.Name}<{Dto.Name}>> Update({Dto.Name} dto, CancellationToken ct)
    {{
        if (dto == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorItemNotSupplied = true }};

        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};{StateManagedProperties}

        var entity = await useCase.FindByIdAsync(dto.Id, ct);
        if (entity == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorItemNotFound = true }};{ImmutableProperties}

        if (!await useCase.CanUpdateAsync(dto, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        mapping.ToEntity(dto, entity);

        if (!await useCase.UpdateAsync(entity, dto, ct))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ ErrorUpdatingState = true }};

        dto = await mapping.ToDtoAsync(entity, dto, ct);

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true,
            Response = dto
        }};
    }}

    public async Task<{BaseResponseT.Name}<bool>> Delete({Entity.KeyProperty.TypeSimpleName} {Dto.Name.ToLower()}{Entity.KeyProperty.Name}, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseResponseT.Name}<bool>() {{ ErrorNotAuthorized = true }};

        var entity = await useCase.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name}, ct);
        if (entity == null)
            return new {BaseResponseT.Name}<bool>() {{ ErrorItemNotFound = true }};

        var dto = await mapping.ToDtoAsync(entity, new {Dto.Name}(), ct);

        if (!await useCase.CanDeleteAsync(dto, ct))
            return new {BaseResponseT.Name}<bool>() {{ ErrorNotAuthorized = true }};
" + (Entity.IsStorageFileUrlProperty ? $@"
        await storageService.DeleteStorageFileAsync(entity, ct);
" : "") + $@"
        if (!await useCase.RemoveAsync(entity, ct))
            return new {BaseResponseT.Name}<bool>() {{ ErrorUpdatingState = true }};

        return new {BaseResponseT.Name}<bool>() 
        {{ 
            Success = true,
            Response = true 
        }};
    }}

    public async Task<{BaseListResponseT.Name}<{Dto.Name}>> List(int? skip, int? take, string[]? orderby, CancellationToken ct)
    {{
        if (!await useCase.IsAllowedAsync(ct))
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        if (!await useCase.CanListAsync(ct))
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorNotAuthorized = true }};

        var entities = useCase.ListAll();

        orderby = orderby == null || orderby.Length == 0 ? [""{Entity.KeyProperty.Name}""] : orderby;
        var dtos = mapping.ProjectToDtosAsync(entities, orderby, skip, take, ct);

        if (dtos == null)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorGettingData = true }};

        return new {BaseListResponseT.Name}<{Dto.Name}>()
        {{
            Success = true,
            Skip = skip ?? 0,
            Take = take ?? 0,
            CanCreate = await useCase.CanCreateAsync(ct),
            Response = dtos
        }};
    }}{loadBysCode}{fileCode}
}}";

        Save();
    }
}