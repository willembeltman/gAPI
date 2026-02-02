using gAPI.CodeGen.Backend.Generators.Shared.StateDtos;
using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

public class StateMappingGenerator : BaseGenerator
{
    public StateMappingGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Core_AuthenticationDirectory;
        Namespace = context.Config.Core_AuthenticationNamespace;

        Context = context;

        Name = "StateMapping";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference IStorageService => Context.SharedReferences.IStorageService;
    public StateDtoGenerator StateUser => Context.StateUser;
    public StateDtoGenerator[] StateObjects => Context.StateObjects;
    public StateDtoGenerator[] AllStateDtos => Context.AllStateObjects;

    public void GenerateCode()
    {
        Reg(IStorageService);

        Code = $@"namespace {Namespace};

public class {Name} ({(StateUser.IsStorageFileUrlProperty || StateObjects.Any(a => a.IsStorageFileUrlProperty) ? $@"
    {IStorageService} storageService" : "")})
{{
{GenerateCodeFor(StateUser)}
{string.Join("", StateObjects.Select(GenerateCodeFor))}
}}";

        Code = GetNamespacesCode() + Code;
        Save(false);
    }

    private string GenerateCodeFor(StateDtoGenerator dto)
    {
        return $@"
    public async Task<{dto.FullName}> ToDtoAsync(
        {dto.Entity.FullName} entity,
        {dto.FullName} dto,
        CancellationToken ct)
    {{{string.Join("", dto.Properties.Select(a => GenerateCodeFor(a)))}{(dto.IsStorageFileUrlProperty ? $@"
        dto.StorageFileUrl = await storageService.GetStorageFileUrlAsync(dto.Id.ToString(), ""{dto.Name}"", ct);" : "")}
        return dto;
    }}
";
    }

    private string GenerateCodeFor(StateDtoPropertyGenerator property)
    {
        if (property.Entity == null)
        {
            return $@"
        dto.{property.Name} = entity.{property.Name};";
        }

        var toDto = AllStateDtos
            .First(a => a.Entity.FullName == property.Entity.FullName);
        if (property.IsList)
        {
            return $@"
        dto.{property.Name} = entity.{property.Name} != null
            ? await Task.WhenAll(entity.{property.Name}.Select(e =>
                ToDtoAsync(e, new {toDto.FullName}(), ct)))
            : [];";
        }
        else
        {
            return $@"
        dto.{property.Name} = entity.{property.Name} != null
            ? await ToDtoAsync(entity.{property.Name}, new {toDto.FullName}(), ct)
            : null;";
        }

        //dto.Id = entity.Id;
        //dto.CurrentCompany = entity.CurrentCompany != null
        //    ? await ToDtoAsync(entity.CurrentCompany, new Shared.StateDtos.StateCompany())
        //    : null;
        //dto.UserName = entity.UserName;
        //dto.Email = entity.Email;
        //dto.CompanyUsers = entity.CompanyUsers != null
        //    ? await Task.WhenAll(entity.CompanyUsers.Select(cu =>
        //        ToDtoAsync(cu, new Shared.StateDtos.StateCompanyUser())))
        //    : [];
    }
}
