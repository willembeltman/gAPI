namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;

public class RegisterResponseGenerator : BaseGenerator
{
    public RegisterResponseGenerator(BackendGenerator context)
    {
        Directory = context.Config.Shared_ResponseDtosDirectory;
        Namespace = context.Config.Shared_ResponseDtosNamespace;

        Context = context;

        Name = $"RegisterResponse";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public void GenerateCode()
    {
        Code = $@"namespace {Namespace};

public class {Name}
{{
    public bool Success {{ get; set; }}
    public bool LockedOut {{ get; set; }}
    public bool ErrorEmailInUse {{ get; set; }}
    public bool ErrorUsernameInUse {{ get; set; }}
    public bool ErrorUsernameEmpty {{ get; set; }}
    public bool ErrorPasswordEmpty {{ get; set; }}
    public bool ErrorPhoneNumberEmpty {{ get; set; }}
    public bool ErrorCouldNotAuthenticateUser {{ get; set; }}
    public bool ErrorEmailEmpty {{ get; set; }}
    public bool ErrorPasswordsDoNotMatch {{ get; set; }}
}}";
        Save(false);
    }
}