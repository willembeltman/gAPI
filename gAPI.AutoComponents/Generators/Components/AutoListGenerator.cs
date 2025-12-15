using gAPI.AutoComponents.Interfaces;
using gAPI.AutoComponents.Models.ServiceModels;
using gAPI.AutoComponents.SimpleRazorCompiler;

namespace gAPI.AutoComponents.Generators.Components;

public class AutoListGenerator : BaseGenerator
{
    public AutoListGenerator(
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference baseListResponseT,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        var iClientAuthenticationService = new SharedReference("gAPI.Interfaces", "IClientAuthenticationService");
        ListGenerator = new ListGenerator(
            crudlType,
            itemDataSource,
            listDataSource,
            iClientAuthenticationService,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}List";
        FileName = $"{Name}.g.cs";

        ListGenerator.Name = Name;
        ListGenerator.FileName = FileName;
    }

    public ListGenerator ListGenerator { get; }

    public void GenerateCode()
    {
        ListGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + ListGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}