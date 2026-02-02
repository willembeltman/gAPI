using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.AutoComponent.SimpleRazorCompiler;
using System;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoSelectListGenerator : BaseGenerator
{
    public AutoSelectListGenerator(
        Generator context,
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace)
    {
        Context = context;
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

        Directory = directory;
        Namespace = @namespace;

        SelectListGenerator.Name = Name;
        SelectListGenerator.FileName = FileName;
    }

    public SelectListGenerator SelectListGenerator { get; }
    public Generator Context { get; }

    public void GenerateCode()
    {
        SelectListGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + SelectListGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}