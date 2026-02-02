using gAPI.AutoPage.Interfaces;
using gAPI.AutoPage.Models.Configs;
using gAPI.AutoPage.Models.CrudlModels;
using gAPI.AutoPage.SimpleRazorCompiler;

namespace gAPI.AutoPage.Generators.Components
{
    public class AutoComponentGenerator : BaseGenerator, IComponent
    {
        public AutoComponentGenerator(Generator generator, CrudlMethod method, PageConfig config)
        {
            Context = generator;
            Generator = new ComponentGenerator(
                generator,
                method,
                this,
                config.Components_Destination.Directory,
                config.Components_Destination.Namespace);

            FileName = $"{Name}.g.cs";
        }

        public Generator Context { get; }
        public ComponentGenerator Generator { get; }

        public override string Namespace => Generator.Namespace;
        public override string Directory => Generator.Directory;
        public override string Name => Generator.Name;
        public bool IsAuthorized => Generator.IsAuthorized;
        public bool IsNotAuthorized => Generator.IsNotAuthorized;
        public string Title => Generator.Title;

        internal void GenerateCode()
        {
            Generator.GenerateCode();

            var razorCode = GetRazorNamespacesCode() + "\r\n" + Generator.Code;
            Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name, Context.SharedReferences.AllComponents);
        }
    }
}