using gAPI.CodeGen.Backend.Config;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Shared.Dtos;

public class StateDtoGenerator : BaseGenerator
{
    public StateDtoGenerator(BackendGenerator context, DirectoryInfo dtoDirectory, string dtoNamespace)
    {
        Context = context;

        Directory = dtoDirectory;
        Namespace = dtoNamespace;

        Name = "State";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public BackendConfig Config => Context.Config;
    public DbContext DbContext => Context.DbContext;

    public void GenerateCode()
    {
        var properties = DbContext.UserEntity.Properties.Where(a => a.IsState)
            .Select(a => $"    public {a.Type.Name}{(a.IsLijst ? "[]" : "")}? {a.Name} {{ get; set; }}\r\n");
        var propertiesCode = string.Join("", properties);

        Code = $@"using gAPI.Attributes;

namespace {Namespace};

[IsStateDto]
public class {Name}
{{
    public {DbContext.UserEntity.Name}? {DbContext.UserEntity.Name} {{ get; set; }}
{propertiesCode}}}";

        Save(true);
    }
}