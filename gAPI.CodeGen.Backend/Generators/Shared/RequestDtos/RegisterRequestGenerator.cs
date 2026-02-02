using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Shared.RequestDtos;

public class RegisterRequestGenerator : BaseGenerator
{
    public RegisterRequestGenerator(BackendGenerator context)
    {
        Directory = context.Config.Shared_RequestDtosDirectory;
        Namespace = context.Config.Shared_RequestDtosNamespace;

        Context = context;

        Name = $"RegisterRequest";
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
    [Required(ErrorMessage = ""Username is required"")]
    public string? UserName {{ get; set; }}

    [Required(ErrorMessage = ""Email is required"")]
    [EmailAddress(ErrorMessage = ""Invalid email address"")]
    public string? Email {{ get; set; }}

    [{IsPassword}]
    [Required(ErrorMessage = ""Password is required"")]
    [MinLength(6, ErrorMessage = ""Password must be at least 6 characters"")]
    public string? Password {{ get; set; }}

    [{IsPassword}]
    [Required(ErrorMessage = ""Please confirm your password"")]
    [Compare(""Password"", ErrorMessage = ""Passwords do not match"")]
    public string? PasswordAgain {{ get; set; }}
}}";
        Save(false);
    }
}