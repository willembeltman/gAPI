using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.CrudlModels;
using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoPage.Generators.Pages
{
    public class AutoPageGenerator : BaseGenerator, IPage
    {
        public AutoPageGenerator(Generator generator, CrudlMethod method)
        {
            Context = generator;
            Generator = new PageGenerator(
                generator,
                method,
                this,
                "Pages",
                "gAPI.Generated.Pages");

            FileName = $"{Name}.g.cs";
        }

        public Generator Context { get; }
        public PageGenerator Generator { get; }
        public string RoutePath => Generator.RoutePath;

        public override string Namespace => Generator.Namespace;
        public override string Directory => Generator.Directory;
        public override string Name => Generator.Name;
        public bool IsAuthorized => Generator.IsAuthorized;
        public bool IsNotAuthorized => Generator.IsNotAuthorized;
        public string Route => Generator.Route;
        public string Title => Generator.Title;

        public void GenerateCode()
        {
            Generator.GenerateCode();

            var razorCode = GetRazorNamespacesCode() + "\r\n" + Generator.Code;
            Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name, Context.SharedReferences.AllComponents);
        }
    }
}