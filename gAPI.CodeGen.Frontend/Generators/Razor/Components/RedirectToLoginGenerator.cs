using gAPI.CodeGen.Frontend.Models;

namespace gAPI.CodeGen.Frontend.Generators.Razor.Components;

public class RedirectToLoginGenerator : BaseGenerator
{
    public RedirectToLoginGenerator(FrontendGenerator generator)
    {
        Generator = generator;
        Imports = generator.Imports;
        BaseResponse = generator.SharedReferences.BaseResponse;

        Namespace = generator.Config.PagesNamespace;
        Directory = generator.Config.PagesDirectory;

        Name = "RedirectToLogin";
        FileName = $"{Name}.razor";
    }

    public FrontendGenerator Generator { get; }
    public ImportsGenerator Imports { get; }
    public SharedReference BaseResponse { get; }

    public void GenerateCode()
    {
        Imports.Reg(BaseResponse);
        Imports.Reg("Microsoft.AspNetCore.Components");

        Code = $@"@inject NavigationManager Navigation

@code {{
    protected override void OnInitialized()
    {{
        Navigation.NavigateTo(""/account/login"");
    }}
}}";

        Save(false);
    }
}