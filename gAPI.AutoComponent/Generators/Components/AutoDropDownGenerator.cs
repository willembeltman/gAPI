using gAPI.AutoComponent.Interfaces;
using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoDropDownGenerator : BaseGenerator
{
    public AutoDropDownGenerator(
        Generator context,
        ICrudlType crudlType,
        ISharedReference listDataSource,
        string directory,
        string @namespace)
    {
        Context = context;
        DropDownGenerator = new DropDownGenerator(
            crudlType,
            listDataSource,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}DropDown";
        FileName = $"{Name}.g.cs";

        Directory = directory;
        Namespace = @namespace;

        DropDownGenerator.Name = Name;
        DropDownGenerator.FileName = FileName;
    }

    public Generator Context { get; }
    public DropDownGenerator DropDownGenerator { get; }

    public void GenerateCode()
    {
        DropDownGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + DropDownGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}
