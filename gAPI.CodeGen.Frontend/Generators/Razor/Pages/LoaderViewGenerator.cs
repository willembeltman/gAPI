using gAPI.CodeGen.Frontend.Models;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Pages;

public class LoaderViewGenerator : BaseGenerator
{
    public LoaderViewGenerator(FrontendGenerator generator)
    {
        Generator = generator;
        Imports = generator.Imports;
        BaseResponse = generator.ServiceContext.BaseResponse;

        Namespace = generator.Config.PagesNamespace;
        Directory = generator.Config.PagesDirectory;

        Name = "LoaderView";
        FileName = $"{Name}.razor";
    }

    public FrontendGenerator Generator { get; }
    public ImportsGenerator Imports { get; }
    public SharedReference BaseResponse { get; }

    public void GenerateCode()
    {
        Imports.Reg(BaseResponse);

        Code = $@"@if (Response?.Success == true)
{{
    @ChildContent
}}
else if (Response?.Success == false)
{{
    <ErrorView Response=""Response"" />
}}
else
{{
    if (LoadingContent != null)
    {{
        @LoadingContent
    }}
    else
    {{
        <p>Loading...</p>
    }}
}}

@code {{
    [Parameter, EditorRequired]
    public {BaseResponse.Name}? Response {{ get; set; }}

    [Parameter]
    public RenderFragment? ChildContent {{ get; set; }}

    [Parameter]
    public RenderFragment? LoadingContent {{ get; set; }}
}}";

        Save();
    }
}