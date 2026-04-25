//using gAPI.AutoComponent.Models;

//namespace gAPI.AutoComponent.Generators.Commen;

//public class ErrorViewGenerator : BaseGenerator
//{
//    public ErrorViewGenerator(Generator generator)
//    {
//        Generator = generator;
//        BaseResponse = generator.SharedReferences.BaseResponse;

//        Directory = "";
//        Namespace = "gAPI.Generated.Components";

//        Name = "ErrorView";
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

//        // Gecompileerde Blazor component met BuildRenderTree
//        Code = @$"{GetNamespacesCode()}
//#nullable enable
//namespace {Namespace}
//{{
//    public partial class {Name} : ComponentBase
//    {{
//        [Parameter]
//        public {BaseResponse.Name}? Response {{ get; set; }}

//        protected override void BuildRenderTree(RenderTreeBuilder __builder)
//        {{
//            var __seq = 0;

//            if (Response != null && Response.Success == false)
//            {{
//                __builder.OpenElement(__seq++, ""div"");
//                __builder.AddAttribute(__seq++, ""class"", ""error-container"");
//                __builder.AddAttribute(__seq++, ""style"", ""border:1px solid #e74c3c; background-color:#fdecea; color:#c0392b; padding:1.5rem; border-radius:8px; max-width:600px;"");

//                __builder.OpenElement(__seq++, ""h2"");
//                __builder.AddAttribute(__seq++, ""style"", ""margin-top:0;"");
//                __builder.AddContent(__seq++, ""An error has occurred"");
//                __builder.CloseElement(); // h2

//                __builder.OpenElement(__seq++, ""p"");
//                __builder.AddContent(__seq++, ""Unfortunately, we were unable to complete your request due to the following issues:"");
//                __builder.CloseElement(); // p

//                __builder.OpenElement(__seq++, ""ul"");
//                __builder.AddAttribute(__seq++, ""style"", ""list-style-type: disc; padding-left:1.5rem;"");

//                if (Response.ErrorAlreadyUsed)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""The item you are trying to use has already been used."");
//                    __builder.CloseElement(); // li
//                }}
//                if (Response.ErrorAttachingState)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""The system was unable to attach the necessary state information."");
//                    __builder.CloseElement();
//                }}
//                if (Response.ErrorGettingState)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""We could not retrieve the current state of the item."");
//                    __builder.CloseElement();
//                }}
//                if (Response.ErrorItemNotFound)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""The requested item could not be found."");
//                    __builder.CloseElement();
//                }}
//                if (Response.ErrorItemNotSupplied)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""No item was provided for this operation."");
//                    __builder.CloseElement();
//                }}
//                if (Response.ErrorNotAuthorized)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""You are not authorized to perform this action."");
//                    __builder.CloseElement();
//                }}
//                if (Response.ErrorUpdatingState)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""There was a problem updating the state of the item."");
//                    __builder.CloseElement();
//                }}
//                if (Response.ErrorGettingData)
//                {{
//                    __builder.OpenElement(__seq++, ""li"");
//                    __builder.AddContent(__seq++, ""There was a problem getting the data of the server."");
//                    __builder.CloseElement();
//                }}

//                __builder.CloseElement(); // ul

//                __builder.OpenElement(__seq++, ""p"");
//                __builder.AddContent(__seq++, ""Please review the above messages and try again. If the problem persists, contact support."");
//                __builder.CloseElement(); // p

//                __builder.OpenElement(__seq++, ""a"");
//                __builder.AddAttribute(__seq++, ""id"", ""backtohome"");
//                __builder.AddAttribute(__seq++, ""href"", ""/"");
//                __builder.AddAttribute(__seq++, ""style"", ""display:inline-block; margin-top:1rem; padding:0.5rem 1rem; background-color:#c0392b; color:white; text-decoration:none; border-radius:4px;"");
//                __builder.AddContent(__seq++, ""⬅ Back to Homepage"");
//                __builder.CloseElement(); // a

//                __builder.CloseElement(); // div
//            }}
//        }}
//    }}
//}}";
//    }
//}
