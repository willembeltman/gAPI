using gAPI.CodeGen.Backend.Generators.Business.CrudHandlers;
using gAPI.CodeGen.Backend.Generators.Business.CrudMappings;
using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Generators.Shared.Interfaces;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Business.CrudServices;

public class CrudServiceGenerator : BaseGenerator
{
    public CrudServiceGenerator(
        CrudServiceInterfaceGenerator serviceInterface,
        DirectoryInfo servicesDirectory,
        string servicesNamespace)
    {
        ServiceInterface = serviceInterface;
        Directory = servicesDirectory;
        Namespace = servicesNamespace;

        CustomMapping = ServiceInterface.CustomMapping;

        Handler = CustomMapping.Handler;

        Dto = Handler.Dto;
        Context = Dto.Context;

        DbSet = Dto.DbSet;
        DbContext = DbSet.DbContext;
        Entity = DbSet.Entity;
        IServerAuthenticationService = Dto.IServerAuthenticationService;
        var AuthenticationState = IServerAuthenticationService.AuthenticationState;
        BaseListResponseT = Context.BaseListResponseT;
        BaseResponseT = Context.BaseResponseT;

        Name = $"{Entity.Name!.ToMultiple()}Service";
        FileName = $"{Name}.cs";
    }

    public CrudServiceInterfaceGenerator ServiceInterface { get; }
    public CrudMappingGenerator CustomMapping { get; }
    public CrudHandlerGenerator Handler { get; }
    public DtoGenerator Dto { get; }
    public BackendGenerator Context { get; }
    public DbSet DbSet { get; }
    public DbContext DbContext { get; }
    public Entity Entity { get; }
    public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference BaseResponseT { get; }

    public void GenerateCode()
    {
        Reg(Dto);
        Reg(Handler);
        Reg(ServiceInterface);
        Reg(BaseResponseT);
        Reg(IServerAuthenticationService);
        Reg("gAPI.AutoMapper");
        Reg("Microsoft.EntityFrameworkCore");
        if (Entity.IsStorageFile)
        {
            Reg("gAPI.Storage");
            Reg("Microsoft.AspNetCore.Http");
        }

        var loadBysCode = string.Join("", Entity.Properties
            .Where(a => a.IsNavigationItem)
            .Select(property =>
            {
                var nav = property.NavigationItemProperty!;
                var navent = nav.Entity;
                var code = $@"

    public async Task<{BaseListResponseT.Name}<{Dto.Name}>> ListBy{nav.Name}({nav.TypeSimpleName} {nav.Name.ToCamelCase()}, int? skip, int? take, string[]? orderby)
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        if (!await handler.CanListAsync())
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        var entities = handler
            .ListAll()
            .Where(a => a.{nav.Name} == {nav.Name.ToCamelCase()});

        orderby ??= [""{Entity.KeyProperty.Name}""];
        var total = await entities.CountAsync();
        var dtos = entities
            .ProjectToDtosAsync(orderby, skip, take, handler);

        if (dtos == null)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorGettingData = true }};

        return new {BaseListResponseT.Name}<{Dto.Name}>()
        {{
            Success = true,
            State = state,
            Skip = skip ?? 0,
            Take = take ?? 0,
            Total = total,
            CanCreate = await handler.CanCreateAsync(),
            Response = dtos
        }};
    }}

    public async Task<{BaseListResponseT.Name}<{Dto.Name}>> ListNotBy{nav.Name}({nav.TypeSimpleName} {nav.Name.ToCamelCase()}, int? skip, int? take, string[]? orderby)
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        if (!await handler.CanListAsync())
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        var entities = handler
            .ListAll()
            .Where(a => a.{nav.Name} != {nav.Name.ToCamelCase()});

        orderby ??= [""{Entity.KeyProperty.Name}""];
        var total = await entities.CountAsync();
        var dtos = entities
            .ProjectToDtosAsync(orderby, skip, take, handler);

        if (dtos == null)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorGettingData = true }};

        return new {BaseListResponseT.Name}<{Dto.Name}>()
        {{
            Success = true,
            State = state,
            Skip = skip ?? 0,
            Take = take ?? 0,
            Total = total,
            CanCreate = await handler.CanCreateAsync(),
            Response = dtos
        }};
    }}";
                return code;
            }));

        var StateManagedProperties = Entity.Properties
            .Where(p => p.StateUserProperty != null)
            .Select(p =>
            {
                if (p.StateUserProperty == null)
                {
                    return $@"
        if (state.{p.Name} != null)
            dto.{p.Name} = state.{p.Name};
";
                }
                return $@"
        if (state.User?.{p.StateUserProperty.Name} != null)
            dto.{p.Name} = state.User.{p.StateUserProperty.Name}{(p.StateUserProperty.IsNullable ? p.StateUserProperty.TypeSimpleName == "string" ? "!" : ".Value" : "")};
";
            });

        var fileCode = "";
        if (Entity.IsStorageFile)
        {
            fileCode += $@"

    public async Task<{BaseResponseT.Name}<{Dto.Name}>> FileUpdate({Entity.KeyProperty.TypeSimpleName} {Dto.Name!.ToLower()}{Entity.KeyProperty.Name}, IFormFile? file)
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};
        {(Entity.IsUser ? "" : string.Join("", StateManagedProperties))}
        var entity = await handler.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name});
        if (entity == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorItemNotFound = true }};

        var dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        if (!await handler.CanUpdateAsync(dto))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        if (file != null)
        {{
            using var storageFileStream = file.OpenReadStream();
            await storageService.SaveStorageFileAsync(entity, file.FileName, file.ContentType, storageFileStream);
        }}

        dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        if (!await handler.UpdateAsync(entity, dto))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorUpdatingState = true }};

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true,
            Response = dto, 
            State = state
        }};
    }}

    public async Task<{BaseResponseT.Name}<bool>> FileDelete({Entity.KeyProperty.TypeSimpleName} {Dto.Name.ToLower()}{Entity.KeyProperty.Name})
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorNotAuthorized = true }};

        var entity = await handler.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name});

        if (entity == null)
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorItemNotFound = true }};

        var dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        if (!await handler.CanDeleteAsync(dto))
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorNotAuthorized = true }};

        await storageService.DeleteStorageFileAsync(entity, false);

        dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        if (!await handler.UpdateAsync(entity, dto))
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorUpdatingState = true }};

        return new {BaseResponseT.Name}<bool>() 
        {{ 
            Success = true,
            State = state, 
            Response = true 
        }};
    }}";
        }

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

public class {Name}(
    {DbContext.FullName} db,
    {IServerAuthenticationService.Name} serverAuthenticationService{(Entity.IsStorageFile ? @",
    IStorageService storageService" : "")})
    : {ServiceInterface.Name}
{{
    public async Task<{BaseResponseT.Name}<{Dto.Name}>> Create({Dto.Name} dto)
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        var entity = await handler.FindByMatchAsync(dto);

        if (entity != null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorAlreadyUsed = true }};
{string.Join("", StateManagedProperties)}
        entity = await dto.ToEntityAsync(new {Entity.FullName}());

        if (!await handler.CanCreateAsync(dto))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        if (!await handler.AddAsync(entity))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorAttachingState = true }};

        dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true, 
            State = state, 
            Response = dto 
        }};
    }}

    public async Task<{BaseResponseT.Name}<{Dto.Name}>> Read({Entity.KeyProperty.TypeSimpleName} {Dto.Name!.ToLower()}{Entity.KeyProperty.Name})
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        var entity = await handler.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name});
        if (entity == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorItemNotFound = true }};

        var dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        if (!await handler.CanReadAsync(dto))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true, 
            State = state, 
            Response = dto 
        }};
    }}

    public async Task<{BaseResponseT.Name}<{Dto.Name}>> Update({Dto.Name} dto)
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        if (dto == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorItemNotSupplied = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};
{(Entity.IsUser ? "" : string.Join("", StateManagedProperties))}
        var entity = await handler.FindByIdAsync(dto.Id);
        if (entity == null)
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorItemNotFound = true }};

        if (!await handler.CanUpdateAsync(dto))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        if (!await handler.UpdateAsync(entity, dto))
            return new {BaseResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorUpdatingState = true }};

        dto = await entity.ToDtoAsync(dto, handler);

        return new {BaseResponseT.Name}<{Dto.Name}>() 
        {{ 
            Success = true,
            Response = dto, 
            State = state
        }};
    }}

    public async Task<{BaseResponseT.Name}<bool>> Delete({Entity.KeyProperty.TypeSimpleName} {Dto.Name.ToLower()}{Entity.KeyProperty.Name})
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorNotAuthorized = true }};

        var entity = await handler.FindByIdAsync({Dto.Name.ToLower()}{Entity.KeyProperty.Name});
        if (entity == null)
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorItemNotFound = true }};

        var dto = await entity.ToDtoAsync(new {Dto.Name}(), handler);

        if (!await handler.CanDeleteAsync(dto))
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorNotAuthorized = true }};
" + (Entity.IsStorageFile ? $@"
        await storageService.DeleteStorageFileAsync(entity, false);
" : "") + $@"
        if (!await handler.RemoveAsync(entity))
            return new {BaseResponseT.Name}<bool>() {{ State = state, ErrorUpdatingState = true }};

        return new {BaseResponseT.Name}<bool>() 
        {{ 
            Success = true,
            State = state, 
            Response = true 
        }};
    }}

    public async Task<{BaseListResponseT.Name}<{Dto.Name}>> List(int? skip, int? take, string[]? orderby)
    {{
        var state = await serverAuthenticationService.GetAuthenticationStateAsync();
        if (!state.Success)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorGettingState = true }};

        var handler = new {Handler.Name}(db, state);

        if (!await handler.IsAllowedAsync())
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        if (!await handler.CanListAsync())
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ State = state, ErrorNotAuthorized = true }};

        var entities = handler.ListAll();

        orderby ??= [""{Entity.KeyProperty.Name}""];
        var total = await entities.CountAsync();
        var dtos = entities
            .ProjectToDtosAsync(orderby, skip, take, handler);

        if (dtos == null)
            return new {BaseListResponseT.Name}<{Dto.Name}>() {{ ErrorGettingData = true }};

        return new {BaseListResponseT.Name}<{Dto.Name}>()
        {{
            Success = true,
            State = state,
            Skip = skip ?? 0,
            Take = take ?? 0,
            Total = total,
            CanCreate = await handler.CanCreateAsync(),
            Response = dtos
        }};
    }}{loadBysCode}{fileCode}
}}";

        Save();
    }
}