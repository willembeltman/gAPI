using gAPI.AutoComponent.Interfaces;
using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoDetailsGenerator : BaseGenerator
{
    public AutoDetailsGenerator(
        Generator context,
        ISharedReference itemDataSource,
        ICrudType crudType)
    {
        Context = context;
        DetailsGenerator = new DetailsGenerator(
            crudType,
            itemDataSource,
            this,
            "",
            "gAPI.Generated.Components");

        Name = $"Auto{crudType.Name}Details";
        FileName = $"{Name}.g.cs"; // ongecompileerde Razor view

        Directory = "";
        Namespace = "gAPI.Generated.Components";

        DetailsGenerator.Name = Name;
        DetailsGenerator.FileName = FileName;
    }

    public Generator Context { get; }
    public DetailsGenerator DetailsGenerator { get; }

    public void GenerateCode()
    {
        DetailsGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + DetailsGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}