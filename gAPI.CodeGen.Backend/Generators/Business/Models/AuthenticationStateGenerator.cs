using gAPI.CodeGen.Backend.Generators.Shared.Dtos;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Business.Models;

public class AuthenticationStateGenerator : BaseGenerator
{
    public AuthenticationStateGenerator(BackendGenerator context, DirectoryInfo modelsDirectory, string modelsNamespace)
    {
        Context = context;

        Directory = modelsDirectory;
        Namespace = modelsNamespace;

        Name = "AuthenticationState";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public StateDtoGenerator StateDto => Context.StateDto;
    public DbContext DbContext => Context.DbContext;

    public void GenerateCode()
    {
        var propertyCode = "";

        //Reg(DbContext);
        Reg(DbContext.UserEntity.Type);

        var properties = DbContext.UserEntity.Properties.Where(a => a.IsState);
        foreach (var a in properties)
        {
            Reg(a.Type);
            propertyCode += $"    public {a.Type.Name}{(a.IsLijst ? "[]" : "")}? Db{a.Name} {{ get; set; }}\r\n";
        }

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

public class {Name} : {StateDto.FullName}
{{
    public bool Success {{ get; set; }}
    public {DbContext.UserEntity.Name}? Db{DbContext.UserEntity.Name} {{ get; set; }}
{propertyCode}}}
";

        Save(true);
    }
}