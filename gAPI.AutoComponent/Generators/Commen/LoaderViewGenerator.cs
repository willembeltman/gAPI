using gAPI.AutoComponent.Models;

namespace gAPI.AutoComponent.Generators.Commen;

public class LoaderViewGenerator : BaseGenerator
{
    public LoaderViewGenerator(Generator generator)
    {
        Generator = generator;
        BaseResponse = generator.SharedReferences.BaseResponse;

        Namespace = generator.Config.Components_Destination.Namespace;
        Directory = generator.Config.Components_Destination.Directory;

        Name = "LoaderView";
        FileName = $"{Name}.g.cs";
    }

    public Generator Generator { get; }
    public SharedReference BaseResponse { get; }

    public void GenerateCode()
    {
        Reg("System");
        Reg("Microsoft.AspNetCore.Components");
        Reg("Microsoft.AspNetCore.Components.Rendering");
        Reg(BaseResponse);


        Code = @$"{GetNamespacesCode()}
#nullable enable
namespace {Namespace}
{{
    public partial class {Name} : ComponentBase
    {{
        [Parameter]
        public {BaseResponse.Name}? Response {{ get; set; }}

        [Parameter]
        public RenderFragment? ChildContent {{ get; set; }}

        [Parameter]
        public RenderFragment? LoadingContent {{ get; set; }}

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {{
            var __seq = 0;

            if (Response?.Success == true)
            {{
                if (ChildContent != null)
                {{
                    __builder.AddContent(__seq++, ChildContent);
                }}
            }}
            else if (Response?.Success == false)
            {{
                __builder.OpenComponent<ErrorView>(__seq++);
                __builder.AddAttribute(__seq++, ""Response"", Response);
                __builder.CloseComponent();
            }}
            else
            {{
                if (LoadingContent != null)
                {{
                    __builder.AddContent(__seq++, LoadingContent);
                }}
                else
                {{
                    __builder.OpenElement(__seq++, ""p"");
                    __builder.AddContent(__seq++, ""Loading..."");
                    __builder.CloseElement();
                }}
            }}
        }}
    }}
}}";
    }
}
