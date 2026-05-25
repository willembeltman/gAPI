using gAPI.AutoComponent.Interfaces;
using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoGridEditGenerator : BaseGenerator
{
    public AutoGridEditGenerator(
        Generator context,
        ICrudType crudType,
        ISharedReference listDataSource,
        string directory,
        string @namespace)
    {
        Context = context;
        GridEditGenerator = new GridEditGenerator(
            crudType,
            listDataSource,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudType.Name}GridEdit";
        FileName = $"{Name}.g.cs";

        Directory = directory;
        Namespace = @namespace;

        GridEditGenerator.Name = Name;
        GridEditGenerator.FileName = FileName;
    }

    public Generator Context { get; }
    public GridEditGenerator GridEditGenerator { get; }

    public void GenerateCode()
    {
        GridEditGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + GridEditGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}
