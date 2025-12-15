using gAPI.AutoComponents.Interfaces;
using gAPI.AutoComponents.SimpleRazorCompiler;

namespace gAPI.AutoComponents.Generators.Components;

public class AutoGridEditGenerator : BaseGenerator
{
    public AutoGridEditGenerator(
        ICrudlType crudlType,
        ISharedReference iClientAuthenticationService,
        ISharedReference itemDataSource,
        ISharedReference listDataSource,
        ISharedReference formFile,
        ISharedReference toFormFileAsyncExtention,
        string directory,
        string @namespace) : base(directory, @namespace)
    {
        GridEditGenerator = new GridEditGenerator(
            crudlType,
            listDataSource,
            this,
            directory,
            @namespace);

        Name = $"Auto{crudlType.Name}GridEdit";
        FileName = $"{Name}.g.cs";

        GridEditGenerator.Name = Name;
        GridEditGenerator.FileName = FileName;
    }

    public GridEditGenerator GridEditGenerator { get; }

    public void GenerateCode()
    {
        GridEditGenerator.GenerateCode();
        var razorCode = GetRazorNamespacesCode() + "\r\n" + GridEditGenerator.Code;
        Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name);
    }
}
