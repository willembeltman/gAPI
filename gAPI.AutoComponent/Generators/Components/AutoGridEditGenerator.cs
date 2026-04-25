using gAPI.AutoComponent.Interfaces;
using gAPI.SimpleRazorCompiler;
using System;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoGridEditGenerator : BaseGenerator
{
    public AutoGridEditGenerator(
        Generator context,
        ICrudlType crudlType,
        ISharedReference listDataSource,
        string directory,
        string @namespace)
    {
        Context = context;
        GridEditGenerator = new GridEditGenerator(
            crudlType,
            listDataSource,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}GridEdit";
        FileName = $"{Name}.g.cs";

        Directory = directory;
        Namespace = @namespace;

        GridEditGenerator.Name = Name;
        GridEditGenerator.FileName = FileName;
    }

    public Generator Context { get; }
    public GridEditGenerator GridEditGenerator { get; }

    public void GenerateCode()
    {
        GridEditGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + GridEditGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}
