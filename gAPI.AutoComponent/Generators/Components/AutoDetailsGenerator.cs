using gAPI.AutoComponent.Interfaces;
using gAPI.AutoComponent.SimpleRazorCompiler;
using System;

namespace gAPI.AutoComponent.Generators.Components;

public class AutoDetailsGenerator : BaseGenerator
{
    public AutoDetailsGenerator(
        Generator context,
        ISharedReference itemDataSource,
        ICrudlType crudlType)
    {
        Context = context;
        DetailsGenerator = new DetailsGenerator(
            crudlType,
            itemDataSource,
            this,
            context.Config.Components_Destination.Directory,
            context.Config.Components_Destination.Namespace);

        Name = $"Auto{crudlType.Name}Details";
        FileName = $"{Name}.g.cs"; // ongecompileerde Razor view

        Directory = context.Config.Components_Destination.Directory;
        Namespace = context.Config.Components_Destination.Namespace;

        DetailsGenerator.Name = Name;
        DetailsGenerator.FileName = FileName;
    }

    public Generator Context { get; }
    public DetailsGenerator DetailsGenerator { get; }

    public void GenerateCode()
    {
        DetailsGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + DetailsGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace!, Name, Context.SharedReferences.AllComponents);
    }
}