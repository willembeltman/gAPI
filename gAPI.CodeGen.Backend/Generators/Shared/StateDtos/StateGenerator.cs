using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Shared.StateDtos;

public class StateGenerator : BaseGenerator
{
    public StateGenerator(
        BackendGenerator context,
        StateDtoGenerator user)
    {
        Directory = context.Config.Shared_StateDtosDirectory;
        Namespace = context.Config.Shared_StateDtosNamespace;

        Context = context;
        User = user;

        Name = "State";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public DbContext DbContext => Context.DbContext;

    public StateDtoGenerator User { get; }
    //public StateDtoGenerator[] StateObjects { get; }
    public SharedReference IsStateDto => Context.SharedReferences.IsStateDto;

    public void GenerateCode()
    {
        Reg(User);
        Reg(IsStateDto);
        Reg("Microsoft.Extensions.Primitives");
        Reg("System.Text");
        Reg("System.Text.Json");
        Reg("System.Text.Json.Serialization");

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[{IsStateDto}]
public class {Name}
{{
    public {User}? User {{ get; set; }}
}}";

        Save(false);
    }
}