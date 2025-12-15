using gAPI.CodeGen.Backend.Generators.Business.CrudMappings;
using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
using gAPI.CodeGen.Backend.Generators.Business.Models;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Shared.Interfaces;

public class CrudServiceInterfaceGenerator : BaseGenerator
{
    public CrudServiceInterfaceGenerator(
        CrudMappingGenerator customMapping,
        DirectoryInfo dtoMappingsDirectory,
        string dtoMappingsNamespace)
    {
        CustomMapping = customMapping;

        Dto = CustomMapping.Dto;
        DbSet = Dto.DbSet;
        Context = Dto.Context;
        Entity = DbSet.Entity;
        IServerAuthenticationService = Dto.IServerAuthenticationService;
        AuthenticationState = IServerAuthenticationService.AuthenticationState;
        BaseResponseT = Context.BaseResponseT;
        BaseListResponseT = Context.BaseListResponseT;
        StateDto = Context.StateDto;
        DbContext = Context.DbContext;

        Directory = dtoMappingsDirectory;
        Namespace = dtoMappingsNamespace;

        Name = $"I{Entity.Name!.ToMultiple()}Service";
        FileName = $"{Name}.cs";
    }

    public CrudMappingGenerator CustomMapping { get; }
    public DtoGenerator Dto { get; }
    public DbSet DbSet { get; }
    public BackendGenerator Context { get; }
    public Entity Entity { get; }
    public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
    public AuthenticationStateGenerator AuthenticationState { get; private set; }
    public BaseListResponseTGenerator BaseListResponseT { get; }
    public BaseResponseTGenerator BaseResponseT { get; }
    public StateDtoGenerator StateDto { get; }
    public DbContext DbContext { get; }

    public void GenerateCode()
    {
        Reg(BaseResponseT);
        Reg(Dto);
        Reg(Dto.Entity.KeyProperty);
        if (Entity.IsStorageFile)
        {
            Reg("Microsoft.AspNetCore.Http");
        }
        Reg("gAPI.Attributes");

        var loadBysCode = string.Join("", Entity.Properties
            .Where(a => a.IsNavigationItem)
            .Select(property =>
            {
                Reg(property.NavigationItemProperty!);
                var nav = property.NavigationItemProperty!;
                var navent = nav.Entity;
                var code = $@"

    [IsListBy(nameof({Dto.Name}.{nav.Name}), typeof({property.Type.Name}))]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> ListBy{property.NavigationItemProperty!.Name}({property.NavigationItemProperty!.TypeSimpleName} {property.NavigationItemProperty!.Name}, int? skip = null, int? take = null, string[]? orderby = null);

    [IsListNotBy(nameof({Dto.Name}.{nav.Name}), typeof({property.Type.Name}))]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> ListNotBy{property.NavigationItemProperty!.Name}({property.NavigationItemProperty!.TypeSimpleName} {property.NavigationItemProperty!.Name}, int? skip = null, int? take = null, string[]? orderby = null);";
                return code;
            }));

        if (Entity.IsStorageFile)
        {
            Code = $@"{GetNamespacesCode()}namespace {Namespace};

[Generate]{(Entity.IsAuthorize ? "\r\n[IsAuthorized]" : "")}
public interface {Name}
{{
    [IsCreate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Create({Dto.Entity.Name} {Dto.Entity.Name.ToLower()});

    [IsRead]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Read({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name});

    [IsUpdate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Update({Dto.Entity.Name} {Dto.Entity.Name.ToLower()});

    [IsDelete(typeof({Dto.Name}))]
    Task<{BaseResponseT.Name}<bool>> Delete({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name});

    [IsList]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> List(int? skip = null, int? take = null, string[]? orderby = null);{loadBysCode}

    [IsFileUpdate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> FileUpdate({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, IFormFile? file);

    [IsFileDelete(typeof({Dto.Name}))]
    Task<{BaseResponseT.Name}<bool>> FileDelete({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name});
}}";
        }
        else
        {
            Code = $@"{GetNamespacesCode()}namespace {Namespace};

[Generate]{(Entity.IsAuthorize ? "\r\n[IsAuthorized]" : "")}
public interface {Name}
{{
    [IsCreate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Create({Dto.Entity.Name} {Dto.Entity.Name.ToLower()});

    [IsRead]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Read({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name});

    [IsUpdate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Update({Dto.Entity.Name} {Dto.Entity.Name.ToLower()});

    [IsDelete(typeof({Dto.Name}))]
    Task<{BaseResponseT.Name}<bool>> Delete({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name});

    [IsList]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> List(int? skip = null, int? take = null, string[]? orderby = null);{loadBysCode}
}}";
        }
        Save(true);
    }

}