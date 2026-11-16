//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Shared.RequestDtos;

//public class LoginRequestGenerator : BaseGenerator
//{
//    public LoginRequestGenerator(BackendGenerator context)
//    {
//        Directory = context.Config.Shared_RequestDtosDirectory;
//        Namespace = context.Config.Shared_RequestDtosNamespace;

//        Context = context;

//        Name = $"LoginRequest";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference IsPassword => Context.SharedReferences.IsPassword;

//    public void GenerateCode()
//    {
//        Reg("System.ComponentModel.DataAnnotations");

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public class {Name}
//{{
//    [Required(ErrorMessage = ""User name is required."")]
//    [EmailAddress(ErrorMessage = ""Invalid email address format."")]
//    public string Email {{ get; set; }} = string.Empty;

//    [{IsPassword}]
//    [Required(ErrorMessage = ""Password is required."")]
//    public string Password {{ get; set; }} = string.Empty;
//}}";
//        Save(false);
//    }
//}