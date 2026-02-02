using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Shared.RequestDtos;

public class ChangePasswordRequestGenerator : BaseGenerator
{
    public ChangePasswordRequestGenerator(BackendGenerator context)
    {
        Directory = context.Config.Shared_RequestDtosDirectory;
        Namespace = context.Config.Shared_RequestDtosNamespace;

        Context = context;

        Name = $"ChangePasswordRequest";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference IsPassword => Context.SharedReferences.IsPassword;

    public void GenerateCode()
    {
        Reg("System.ComponentModel.DataAnnotations");

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}
{{
    [{IsPassword}]
    [Required(ErrorMessage = ""Password is required."")]
    public string Password {{ get; set; }} = string.Empty;

    [{IsPassword}]
    [Required(ErrorMessage = ""NewPassword is required."")]
    public string NewPassword {{ get; set; }} = string.Empty;

    [{IsPassword}]
    [Required(ErrorMessage = ""NewPasswordRepeat is required."")]
    public string NewPasswordRepeat {{ get; set; }} = string.Empty;
}}";
        Save(false);
    }
}