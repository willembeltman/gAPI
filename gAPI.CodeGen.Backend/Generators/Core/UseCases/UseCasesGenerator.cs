//using gAPI.CodeGen.Backend.Generators.Core.Authentication;
using gAPI.CodeGen.Backend.Generators.Shared.Public.Dtos;
using gAPI.CodeGen.Backend.Helpers;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Core.CrudHandlers;

public class UseCasesGenerator : BaseGenerator
{
    public UseCasesGenerator(
        BackendGenerator context,
        DtoGenerator dto)
    {
        Directory = context.Config.Core_CrudUseCasesDirectory;
        Namespace = context.Config.Core_CrudUseCasesNamespace;

        Context = context;
        Dto = dto;

        Name = $"{Entity.Name.ToMultiple()}UseCase";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DtoGenerator Dto { get; }

    public DbSet DbSet => Dto.DbSet;
    public Entity Entity => Dto.Entity;
    public DbContext DbContext => Context.DbContext;

    public SharedReference IUseCase => Context.SharedReferences.IUseCase;
    public SharedReference IAuthenticationService => Context.SharedReferences.IAuthenticationService;
    public SharedReference StateDto => Context.State;
    public SharedReference User => Context.DbContext.UserEntity;

    public void GenerateCode()
    {
        var keyType = Entity.Properties.FirstOrDefault(a => a.IsKey)?.TypeSimpleName ?? "long";

        Reg(User);
        Reg(IAuthenticationService);
        Reg(DbContext.Type);
        //Reg(Entity.Type);
        Reg("Microsoft.EntityFrameworkCore");

        var attachCode = @$" 
    {{
        await db.{DbSet.Name}.AddAsync(entityToAdd, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }}";
        //    if (Entity.Properties.Any(a => a.IsStateManaged != null))
        //    {
        //        attachCode = @$"
        //{{
        //    if (entity == null) return false;
        //    if (authenticationService.State.User == null) return false;{string.Join("", Entity.Properties
        //        .Where(a => a.IsStateManaged != null)
        //        .Select(prop => @$"
        //    {(prop.IsStateManaged!.CheckForNull ? $"if (authenticationService.State.{prop.IsStateManaged!.Name} == null) return false;" : "")}
        //    entity.{prop.Name} = authenticationService.State.{prop.IsStateManaged!.Name}{(prop.IsStateManaged.UseValue ? ".Value" : "")};"))}
        //    await db.{DbSet.Name}.AddAsync(entity, ct);
        //    await db.SaveChangesAsync(ct);
        //    return true;
        //}}";
        //    }

        var authenticated = Dto.Entity.IsAuthorize ? "authenticationService.State.User != null" : "true";

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}(
    {DbContext.Name} db,
    IAuthenticationService<{User.FullName}, {StateDto.FullName}> authenticationService)
    : {IUseCase.FullName}<{Entity.FullName}, {Dto.FullName}, {Entity.KeyProperty.TypeSimpleName}>
{{
    public async Task<bool> IsAllowedAsync(CancellationToken ct) => {authenticated};
    public async Task<bool> CanListAsync(CancellationToken ct) => {authenticated};
    public async Task<bool> CanCreateAsync(CancellationToken ct) => authenticationService.State.User != null;
    public async Task<bool> CanCreateAsync({Dto.FullName} dto, CancellationToken ct) => authenticationService.State.User != null;
    public async Task<bool> CanReadAsync({Dto.FullName} dto, CancellationToken ct) => {authenticated};
    public async Task<bool> CanUpdateAsync({Dto.FullName} dto, CancellationToken ct) => authenticationService.State.User != null;
    public async Task<bool> CanDeleteAsync({Dto.FullName} dto, CancellationToken ct) => authenticationService.State.User != null;

    public async Task<{Entity.Name}?> FindByMatchAsync({Dto.FullName} dto, CancellationToken ct) {(Entity.Properties.Any(a => a.IsName != null) ? $@"
        => await db.{DbSet.Name}{string.Join("", Entity.ForeignKeyProperties.Select(p => $@"
            .Include(""{p.Name}"")"))} // Add your filter query
            .FirstOrDefaultAsync(a => {string.Join(" &&", Entity.Properties.Where(p => p.IsName != null).Select(p => $@"
                a.{p.Name} == dto.{p.Name}"))}, ct);" : $@"
        => null; // If you implement this, also use includes")}
    public async Task<{Entity.Name}?> FindByIdAsync({keyType} id, CancellationToken ct) 
        => await db.{DbSet.Name}{string.Join("", Entity.ForeignKeyProperties.Select(p => $@"
            .Include(""{p.Name}"")"))} // Add your filter query
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    public IQueryable<{Entity.Name}> ListAll()
        => db.{DbSet.Name}; // Add your filter query, no need for includes here

    public async Task<bool> AddAsync({Entity.Name} entityToAdd, CancellationToken ct){attachCode}
    public async Task<bool> UpdateAsync({Entity.Name} updatedEntity, {Dto.FullName} dto, CancellationToken ct)
    {{
        await db.SaveChangesAsync();
        return true;
    }}
    public async Task<bool> RemoveAsync({Entity.Name} entity, CancellationToken ct)
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