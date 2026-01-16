using gAPI.CodeGen.Backend.Generators.Business.Interfaces;
using gAPI.CodeGen.Backend.Generators.Business.Models;
using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Business.CrudHandlers;

public class CrudHandlerGenerator : BaseGenerator
{
    public CrudHandlerGenerator(
        DtoGenerator dto,
        DirectoryInfo directory,
        string @namespace)
    {
        Directory = directory;
        Namespace = @namespace;

        Dto = dto;
        DbSet = Dto.DbSet;
        Entity = DbSet.Entity;

        Context = Dto.Context;
        IServerAuthenticationService = Context.IServerAuthenticationService;
        AuthenticationState = IServerAuthenticationService.AuthenticationState;
        BaseResponseT = Context.BaseResponseT;

        StateDto = Context.StateDto;
        DbContext = StateDto.DbContext;

        Name = $"{Entity.Name!.ToMultiple()}Handler";
        FileName = $"{Name}.cs";
    }

    public DtoGenerator Dto { get; }
    public DbSet DbSet { get; }
    public Entity Entity { get; }
    public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
    public BackendGenerator Context { get; }
    public AuthenticationStateGenerator AuthenticationState { get; }
    public SharedReference BaseResponseT { get; }
    public StateDtoGenerator StateDto { get; }
    public DbContext DbContext { get; }

    public void GenerateCode()
    {
        var keyType = Entity.Properties.FirstOrDefault(a => a.IsKey)?.TypeSimpleName ?? "long";

        Reg(AuthenticationState);
        Reg(DbContext.Type);
        Reg(Entity.Type);
        Reg("Microsoft.EntityFrameworkCore");
        Reg("gAPI.AutoComparer");
        Reg("gAPI.AutoMapper");

        var attachCode = @$" 
    {{
        await db.{DbSet.Name}.AddAsync(entity);
        await db.SaveChangesAsync();
        return true;
    }}";
        if (Entity.Properties.Any(a => a.StateUserProperty != null))
        {
            attachCode = @$"
    {{
        if (entity == null) return false;
        if (AuthenticationState == null) return false;
        if (AuthenticationState.User == null) return false;{string.Join("", Entity.Properties
            .Where(a => a.StateUserProperty != null)
            .Select(prop => @$"
        {(prop.StateUserProperty!.IsNullable && prop.StateUserProperty.IsValueType ? $"if (AuthenticationState.User.{prop.StateUserProperty!.Name} == null) return false;" : "")}
        entity.{prop.Name} = AuthenticationState.User.{prop.StateUserProperty!.Name}{(prop.StateUserProperty.IsNullable && prop.StateUserProperty.IsValueType ? ".Value" : "")};"))}
        await db.{DbSet.Name}.AddAsync(entity);
        await db.SaveChangesAsync();
        return true;
    }}";
        }

        var authenticated = Dto.Entity.IsAuthorize ? "AuthenticationState!.DbUser != null" : "true";

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

public class {Name}({DbContext.Name} db, {AuthenticationState.Name}? AuthenticationState) : ISecurityHandler<{Dto.FullName}>
{{
    public async Task<bool> IsAllowedAsync() => await Task.FromResult({authenticated});
    public async Task<bool> CanListAsync() => await Task.FromResult({authenticated});
    public async Task<bool> CanCreateAsync() => await Task.FromResult(AuthenticationState!.DbUser != null);
    public async Task<bool> CanCreateAsync({Dto.FullName} dto) => await Task.FromResult(AuthenticationState!.DbUser != null);
    public async Task<bool> CanReadAsync({Dto.FullName} dto) => await Task.FromResult({authenticated});
    public async Task<bool> CanUpdateAsync({Dto.FullName} dto) => await Task.FromResult(AuthenticationState!.DbUser != null);
    public async Task<bool> CanDeleteAsync({Dto.FullName} dto) => await Task.FromResult(AuthenticationState!.DbUser != null);

    public async Task<{Entity.Name}?> FindByMatchAsync({Dto.FullName} dto) => await Task.FromResult(({Entity.Name}?)null);
    public async Task<{Entity.Name}?> FindByIdAsync({keyType} id) => await ListAll().FirstOrDefaultAsync(a => a.Id == id);
    public IQueryable<{Entity.Name}> ListAll() => db.{DbSet.Name};

    public async Task<bool> AddAsync({Entity.Name} entity){attachCode}
    public async Task<bool> UpdateAsync({Entity.Name} entity, {Dto.FullName} dto)
    {{
        if (!dto.IsEqualTo(entity))
        {{
            entity = await dto.ToEntityAsync(entity);
            await db.SaveChangesAsync();
        }}
        return true;
    }}
    public async Task<bool> RemoveAsync({Entity.Name} entity)
    {{
        db.{DbSet.Name}.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }}
}}
";
        Save(false);
    }
}