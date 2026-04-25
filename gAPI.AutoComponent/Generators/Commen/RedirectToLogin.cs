//using gAPI.AutoComponent.Models;

//namespace gAPI.AutoComponent.Generators.Commen;

//public class RedirectToLoginGenerator : BaseGenerator
//{
//    public RedirectToLoginGenerator(Generator generator)
//    {
//        Generator = generator;
//        BaseResponse = generator.SharedReferences.BaseResponse;

//        Directory = "";
//        Namespace = "gAPI.Generated.Components";

//        Name = "RedirectToLogin";
//        FileName = $"{Name}.g.cs";
//    }

//    public Generator Generator { get; }
//    public SharedReference BaseResponse { get; }

//    public void GenerateCode()
//    {
//        Reg("System");
//        Reg("Microsoft.AspNetCore.Components");
//        Reg("Microsoft.AspNetCore.Components.Rendering");
//        Reg(BaseResponse);

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
//            Navigation.NavigateTo(""/account/login"");
//        }}

//        protected override void BuildRenderTree(RenderTreeBuilder __builder)
//        {{
//            // No UI content, only redirect
//        }}
//    }}
//}}";
//    }
//}
