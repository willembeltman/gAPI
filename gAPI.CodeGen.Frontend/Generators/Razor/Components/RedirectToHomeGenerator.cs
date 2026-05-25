//namespace gAPI.CodeGen.Frontend.Generators.Razor.Components;

//public class RedirectToHomeGenerator : BaseGenerator
//{
//    public RedirectToHomeGenerator(FrontendGenerator generator)
//    {
//        Imports = generator.Imports;

//        Namespace = generator.Config.PagesNamespace;
//        Directory = generator.Config.PagesDirectory;

//        Name = "RedirectToHome";
//        FileName = $"{Name}.razor";
//    }

//    public ImportsGenerator Imports { get; }

//    public void GenerateCode()
//    {
//        Imports.Reg("Microsoft.AspNetCore.Components");
//        Code = $@"@inject NavigationManager Navigation

//@code {{
//    protected override void OnInitialized()
//    {{
//        Navigation.NavigateTo(""/"");
//    }}
//}}";

//        Save(false);
//    }
//}