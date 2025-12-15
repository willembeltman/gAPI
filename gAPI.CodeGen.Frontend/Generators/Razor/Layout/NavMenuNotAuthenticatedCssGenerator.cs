//using gAPI.CodeGen.Frontend.Helpers;
//using gAPI.CodeGen.FrontEnd;

//namespace gAPI.CodeGen.Frontend.Generators;

//public class LayoutNavMenuNotAuthenticatedGenerator : BaseGenerator
//{
//    public LayoutNavMenuNotAuthenticatedGenerator(FrontendGenerator generator)
//    {
//        Generator = generator;

//        Directory = generator.ClientConfig.LayoutDirectory;
//        Namespace = $"{generator.ClientConfig.LayoutNamespace}";

//        Name = "NavMenuNotAuthenticated";
//        FileName = $"{Name}.razor";
//    }

//    public FrontendGenerator Generator { get; }

//    public void GenerateCode()
//    {
//        Code = string.Join("\r\n\r\n", Generator.ListGenerators
//            .Where(a => a.Dto.IsAuthorized == false && a.Dto.IsEntryPoint && a.Dto.ListMethod != null && a.Dto.IsJunction == false)
//            .Select(a => $@"
//<div class=""nav-item px-3"">
//    <NavLink class=""nav-link"" href=""{a.Dto.Name!.ToMultiple().ToLower()}"">
//        <span class=""bi bi-list-nested-nav-menu"" aria-hidden=""true""></span> {a.Dto.Name!.ToMultiple()}
//    </NavLink>
//</div>"));

//        Save();
//    }

//}