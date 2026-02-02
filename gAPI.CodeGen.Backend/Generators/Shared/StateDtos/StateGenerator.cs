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

    public StringValues CreateStateData()
    {{
        var json = JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {{
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }});

        var base64State = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return new StringValues([base64State]);
    }}

    public static State? FromStateData(IEnumerable<string> stateDataValues)
    {{
        string? headerValue = stateDataValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue))
            return null;

        try
        {{
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(headerValue));

            var state = JsonSerializer.Deserialize<State>(json);
            return state;
        }}
        catch
        {{
            return null;
        }}
    }}

    public bool IsDifferent(State? value)
    {{
        if (value == null)
            return true;

        return
            User?.Id != value.User?.Id ||
            User?.UserName != value.User?.UserName ||
            User?.Email != value.User?.Email ||
            User?.StorageFileUrl != value.User?.StorageFileUrl ||
            User?.CurrentCompany?.Id != value.User?.CurrentCompany?.Id ||
            User?.CurrentCompany?.Name != value.User?.CurrentCompany?.Name;
    }}
}}";

        Save(false);
    }
}