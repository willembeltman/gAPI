using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.AutoComponent.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoDropDownGenerator : BaseGenerator
{
    public AutoDropDownGenerator(
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        var iClientAuthenticationService = new SharedReference("gAPI.Interfaces", "IClientAuthenticationService");
        DropDownGenerator = new DropDownGenerator(
            crudlType,
            listDataSource,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}DropDown";
        FileName = $"{Name}.g.cs";

        DropDownGenerator.Name = Name;
        DropDownGenerator.FileName = FileName;
    }

    public DropDownGenerator DropDownGenerator { get; }

    public void GenerateCode()
    {
        DropDownGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + DropDownGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}
