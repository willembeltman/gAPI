using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.Models;
using gAPI.AutoComponent.SimpleRazorCompiler;
using System;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoFormGenerator : BaseGenerator
{
    public AutoFormGenerator(
        Generator context,
        ICrudlType dto,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference formFile,
        ISharedReference toFormFileAsyncExtention,
        string directory,
        string @namespace)
    {
        Context = context;
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
