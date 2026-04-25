using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.AutoComponent.Models.ServiceModels;
using gAPI.SimpleRazorCompiler;
using System;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoTableGenerator : BaseGenerator
{
    public AutoTableGenerator(
        Generator context,
        ICrudlType crudlType,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        string directory,
        string @namespace) 
    {
        Context = context;
        var iClientAuthenticatedHttpClient = new SharedReference("gAPI.Interfaces", "IClientAuthenticatedHttpClient");
        TableGenerator = new TableListGenerator(
            crudlType,
            itemDataSource,
            listDataSource,
            iClientAuthenticatedHttpClient,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}TableList";
        FileName = $"{Name}.g.cs";

        Directory = directory;
        Namespace = @namespace;

        TableGenerator.Name = Name;
        TableGenerator.FileName = FileName;
    }

    public TableListGenerator TableGenerator { get; }
    public Generator Context { get; }

    public void GenerateCode()
    {
        TableGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + TableGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}