using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.AutoComponent.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoTableGenerator : BaseGenerator
{
    public AutoTableGenerator(
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        var iClientAuthenticationService = new SharedReference("gAPI.Interfaces", "IClientAuthenticationService");
        TableGenerator = new TableGenerator(
            crudlType,
            itemDataSource,
            listDataSource,
            iClientAuthenticationService,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}TableList";
        FileName = $"{Name}.g.cs";

        TableGenerator.Name = Name;
        TableGenerator.FileName = FileName;
    }

    public TableGenerator TableGenerator { get; }

    public void GenerateCode()
    {
        TableGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + TableGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}