using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoListGenerator : BaseGenerator
{
    public AutoListGenerator(
        Generator context,
        ICrudType crudType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace)
    {
        Context = context;
        var iClientAuthenticatedHttpClient = new SharedReference("gAPI.Core.Client", "IAuthenticatedHttpClient");
        ListGenerator = new ListGenerator(
            crudType,
            itemDataSource,
            listDataSource,
            iClientAuthenticatedHttpClient,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudType.Name}List";
        FileName = $"{Name}.g.cs";

        Directory = directory;
        Namespace = @namespace;

        ListGenerator.Name = Name;
        ListGenerator.FileName = FileName;
    }

    public ListGenerator ListGenerator { get; }
    public Generator Context { get; }

    public void GenerateCode()
    {
        ListGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + ListGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}