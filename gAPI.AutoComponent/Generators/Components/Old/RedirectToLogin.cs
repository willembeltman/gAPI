//using gAPI.AutoComponents.ServiceModels;

//namespace gAPI.AutoComponents.Generators.Components;

//public class RedirectToLoginGenerator : BaseGenerator
//{
//    public RedirectToLoginGenerator(ViewsGenerator generator)
//    {
//        Generator = generator;
//        BaseResponse = generator.ServiceContext.BaseResponse;

//        Namespace = generator.Config.Components_Destination.Namespace;
//        Directory = generator.Config.Components_Destination.Directory;

//        Name = "RedirectToLogin";
//        FileName = $"{Name}.g.cs";
//    }

//    public ViewsGenerator Generator { get; }
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
