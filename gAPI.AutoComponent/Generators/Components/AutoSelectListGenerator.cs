using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.AutoComponent.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoSelectListGenerator : BaseGenerator
{
    public AutoSelectListGenerator(
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        var iClientAuthenticationService = new SharedReference("gAPI.Interfaces", "IClientAuthenticationService");
        SelectListGenerator = new SelectListGenerator(
            crudlType,
            itemDataSource,
            listDataSource,
            iClientAuthenticationService,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}SelectList";
        FileName = $"{Name}.g.cs";

        SelectListGenerator.Name = Name;
        SelectListGenerator.FileName = FileName;
    }

    public SelectListGenerator SelectListGenerator { get; }

    public void GenerateCode()
    {
        SelectListGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + SelectListGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}