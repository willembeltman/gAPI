using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.AutoComponent.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoFormGenerator : BaseGenerator
{
    public AutoFormGenerator(
        ICrudlType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference formFile,
        ISharedReference toFormFileAsyncExtention,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        var iClientAuthenticationService = new SharedReference("gAPI.Interfaces", "IClientAuthenticationService");
        FormGenerator = new FormGenerator(
            dto,
            itemDataSource,
            listDataSource,
            iClientAuthenticationService,
            formFile, toFormFileAsyncExtention,
            this,
            directory,
            @namespace);

        Name = $"Auto{dto.Name}Form";
        FileName = $"{Name}.g.cs";

        FormGenerator.Name = Name;
        FormGenerator.FileName = FileName;
    }

    public FormGenerator FormGenerator { get; }

    public void GenerateCode()
    {
        FormGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + FormGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}
