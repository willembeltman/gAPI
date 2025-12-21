//using gAPI.AutoComponents.ServiceModels;

//namespace gAPI.AutoComponents.Generators.Components;

//public class RedirectToHomeGenerator : BaseGenerator
//{
//    public RedirectToHomeGenerator(ViewsGenerator generator)
//    {
//        Namespace = generator.Config.Components_Destination.Namespace;
//        Directory = generator.Config.Components_Destination.Directory;

//        Name = "RedirectToHome";
//        FileName = $"{Name}.g.cs";
//    }

//    public void GenerateCode()
//    {
//        Reg("System");
//        Reg("Microsoft.AspNetCore.Components");
//        Reg("Microsoft.AspNetCore.Components.Rendering");
//        Code = @$"{GetNamespacesCode()}
//#nullable enable
//namespace {Namespace}
//{{
//    public partial class {Name} : ComponentBase
//    {{
//        [Inject]
//        public NavigationManager Navigation {{ get; set; }} = default!;

//        protected override void OnInitialized()
//        {{
//            Navigation.NavigateTo(""/"");
//        }}

//        protected override void BuildRenderTree(RenderTreeBuilder __builder)
//        {{
//            // No UI content, only redirect
//        }}
//    }}
//}}";
//    }
//}
