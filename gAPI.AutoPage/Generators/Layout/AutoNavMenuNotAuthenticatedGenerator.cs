using gAPI.AutoPage.Models.Configs;
using gAPI.AutoPage.SimpleRazorCompiler;

namespace gAPI.AutoPage.Generators.Layout
{
    public class AutoNavMenuNotAuthenticatedGenerator : BaseGenerator
    {
        public AutoNavMenuNotAuthenticatedGenerator(
            Generator context,
            PageConfig config)
        {
            Context = context;

            Generator = new NavMenuNotAuthenticatedGenerator(
                context,
                this,
                config.Layout_Destination.Directory,
                config.Layout_Destination.Namespace,
                true);
        }

        public Generator Context { get; }
        public NavMenuNotAuthenticatedGenerator Generator { get; }

        public override string Namespace => Generator.Namespace;
        public override string Directory => Generator.Directory;
        public override string Name => Generator.Name;
        public override string FileName => Generator.FileName;

        internal void GenerateCode()
        {
            Generator.GenerateCode();

            var razorCode = GetRazorNamespacesCode() + "\r\n" + Generator.Code;
            Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name, Context.SharedReferences.AllComponents);
        }
    }
}