using gAPI.SimpleRazorCompiler;

namespace gAPI.AutoPage.Generators.Layout
{
    public class AutoNavMenuAuthenticatedGenerator : BaseGenerator
    {
        public AutoNavMenuAuthenticatedGenerator(
            Generator context)
        {
            Context = context;

            Generator = new NavMenuAuthenticatedGenerator(
                context,
                this,
                "Layout",
                "gAPI.Generated.Layout",
                true);
        }

        public Generator Context { get; }
        public NavMenuAuthenticatedGenerator Generator { get; }

        public override string Namespace => Generator.Namespace;
        public override string Directory => Generator.Directory;
        public override string Name => Generator.Name;
        public override string FileName => Generator.FileName;

        public void GenerateCode()
        {
            Generator.GenerateCode();

            var razorCode = GetRazorNamespacesCode() + "\r\n" + Generator.Code;
            Code = RazorCompiler.CompileRazorToComponent(razorCode, Namespace, Name, Context.SharedReferences.AllComponents);
        }
    }
}