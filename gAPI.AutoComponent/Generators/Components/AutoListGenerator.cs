using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.AutoComponent.SimpleRazorCompiler;
using System;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoListGenerator : BaseGenerator
{
    public AutoListGenerator(
        Generator context,
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace)
    {
        Context = context;
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