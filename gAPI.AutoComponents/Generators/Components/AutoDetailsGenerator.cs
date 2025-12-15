using gAPI.AutoComponents.Interfaces;
using gAPI.AutoComponents.SimpleRazorCompiler;

namespace gAPI.AutoComponents.Generators.Components;

public class AutoDetailsGenerator : BaseGenerator
{
    public AutoDetailsGenerator(
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        DetailsGenerator = new DetailsGenerator(
            crudlType,
            itemDataSource,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}Details";
        FileName = $"{Name}.g.cs"; // ongecompileerde Razor view

        DetailsGenerator.Name = Name;
        DetailsGenerator.FileName = FileName;
    }

    public DetailsGenerator DetailsGenerator { get; }

    public void GenerateCode()
    {
        DetailsGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + DetailsGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}