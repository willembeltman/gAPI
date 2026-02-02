namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;

public class LoginResponseGenerator : BaseGenerator
{
    public LoginResponseGenerator(BackendGenerator context)
    {
        Directory = context.Config.Shared_ResponseDtosDirectory;
        Namespace = context.Config.Shared_ResponseDtosNamespace;

        Context = context;

        Name = $"LoginResponse";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public void GenerateCode()
    {
        Code = $@"namespace {Namespace};

public class {Name}
{{
    public bool Success {{ get; set; }}
    public bool ErrorLockedOut {{ get; set; }}
}}";
        Save(false);
    }
}