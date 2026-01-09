namespace gAPI.AutoComponent.Generators.Helpers;

public class StateChangedHandlerGenerator : BaseGenerator
{
    public StateChangedHandlerGenerator(
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        Name = "StateChangedHandler";
        FileName = $"{Name}.g.cs";
    }
    public void GenerateCode()
    {
        Code = $@"
namespace {Namespace}
{{
    public delegate void {Name}();
}}
";
    }
}
