//using gAPI.CodeGen.Backend.Generators.Core.Authentication;
using gAPI.CodeGen.Backend.Generators.Core.CrudMappings;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Generators.Shared.StateDtos;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Shared.Interfaces;

public class CrudServiceInterfaceGenerator : BaseGenerator
{
    public CrudServiceInterfaceGenerator(
        BackendGenerator context,
        DtoGenerator dto)
    {
        Directory = context.Config.Shared_InterfacesDirectory;
        Namespace = context.Config.Shared_InterfacesNamespace;

        Context = context;
        Dto = dto;

        Name = $"I{Entity.Name!.ToMultiple()}Service";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DtoGenerator Dto { get; }

    public CrudMappingGenerator CrudMapping => Dto.CrudMapping;
    public DbSet DbSet => Dto.DbSet;
    public Entity Entity => Dto.Entity;
    //public IServerAuthenticationServiceGenerator IServerAuthenticationService => Context.IServerAuthenticationService;
    //public ServerAuthenticationStateGenerator AuthenticationState => Context.ServerAuthenticationState;
    public SharedReference BaseListResponseT => Context.SharedReferences.BaseListResponseT;
    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;
    public StateGenerator State => Context.State;
    public DbContext DbContext => Context.DbContext;

    public void GenerateCode()
    {
        Reg(BaseResponseT);
        Reg(Dto);
        Reg(Dto.Entity.KeyProperty);
        if (Entity.IsStorageFileUrlProperty)
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
                var navent = nav.ParentEntity;
                var code = $@"

    [IsListBy(nameof({Dto.Name}.{nav.Name}), typeof({property.Type.Name}))]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> ListBy{property.NavigationItemProperty!.Name}({property.NavigationItemProperty!.TypeSimpleName} {property.NavigationItemProperty!.Name}, int? skip, int? take, string[]? orderby, CancellationToken ct);

    [IsListNotBy(nameof({Dto.Name}.{nav.Name}), typeof({property.Type.Name}))]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> ListNotBy{property.NavigationItemProperty!.Name}({property.NavigationItemProperty!.TypeSimpleName} {property.NavigationItemProperty!.Name}, int? skip, int? take, string[]? orderby, CancellationToken ct);";
                return code;
            }));

        if (Entity.IsStorageFileUrlProperty)
        {
            Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[GenerateApi]{(Entity.IsAuthorize ? "\r\n[IsAuthorized]" : "")}
public interface {Name}
{{
    [IsCreate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Create({Dto.Entity.Name} {Dto.Entity.Name.ToLower()}, CancellationToken ct);

    [IsRead]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Read({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, CancellationToken ct);

    [IsUpdate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Update({Dto.Entity.Name} {Dto.Entity.Name.ToLower()}, CancellationToken ct);

    [IsDelete(typeof({Dto.Name}))]
    Task<{BaseResponseT.Name}<bool>> Delete({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, CancellationToken ct);

    [IsList]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> List(int? skip, int? take, string[]? orderby, CancellationToken ct);{loadBysCode}

    [IsFileUpdate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> FileUpdate({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, IFormFile? file, CancellationToken ct);

    [IsFileDelete(typeof({Dto.Name}))]
    Task<{BaseResponseT.Name}<bool>> FileDelete({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, CancellationToken ct);
}}";
        }
        else
        {
            Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[GenerateApi]{(Entity.IsAuthorize ? "\r\n[IsAuthorized]" : "")}
public interface {Name}
{{
    [IsCreate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Create({Dto.Entity.Name} {Dto.Entity.Name.ToLower()}, CancellationToken ct);

    [IsRead]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Read({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, CancellationToken ct);

    [IsUpdate]
    Task<{BaseResponseT.Name}<{Dto.Name}>> Update({Dto.Entity.Name} {Dto.Entity.Name.ToLower()}, CancellationToken ct);

    [IsDelete(typeof({Dto.Name}))]
    Task<{BaseResponseT.Name}<bool>> Delete({Dto.Entity.KeyProperty.TypeSimpleName} {Dto.Entity.Name.ToLower()}{Dto.Entity.KeyProperty.Name}, CancellationToken ct);

    [IsList]
    Task<{BaseListResponseT.Name}<{Dto.Name}>> List(int? skip, int? take, string[]? orderby, CancellationToken ct);{loadBysCode}
}}";
        }
        Save(false);
    }

}