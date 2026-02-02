namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;

public class ForgotPasswordResponseGenerator : BaseGenerator
{
    public ForgotPasswordResponseGenerator(BackendGenerator context)
    {
        Directory = context.Config.Shared_ResponseDtosDirectory;
        Namespace = context.Config.Shared_ResponseDtosNamespace;

        Context = context;

        Name = $"ForgotPasswordResponse";
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