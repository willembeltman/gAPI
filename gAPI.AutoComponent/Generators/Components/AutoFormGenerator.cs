using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoFormGenerator : BaseGenerator
{
    public AutoFormGenerator(
        Generator context,
        ICrudlType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference formFile,
        ISharedReference toFormFileAsyncExtension,
        string directory,
        string @namespace)
    {
        Context = context;
        var iClientAuthenticatedHttpClient = new SharedReference("gAPI.Interfaces", "IClientAuthenticatedHttpClient");
        FormGenerator = new FormGenerator(
            dto,
            itemDataSource,
            listDataSource,
            iClientAuthenticatedHttpClient,
            formFile, toFormFileAsyncExtension,
            this,
            directory,
            @namespace);

        Name = $"Auto{dto.Name}Form";
        FileName = $"{Name}.g.cs";

        Directory = directory;
        Namespace = @namespace;

        FormGenerator.Name = Name;
        FormGenerator.FileName = FileName;
    }

    public FormGenerator FormGenerator { get; }
    public Generator Context { get; }

    public void GenerateCode()
    {
        FormGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n\r\n" + FormGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}
