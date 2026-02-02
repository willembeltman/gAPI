namespace gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;

public class ChangePasswordResponseGenerator : BaseGenerator
{
    public ChangePasswordResponseGenerator(BackendGenerator context)
    {
        Directory = context.Config.Shared_ResponseDtosDirectory;
        Namespace = context.Config.Shared_ResponseDtosNamespace;

        Context = context;

        Name = $"ChangePasswordResponse";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public void GenerateCode()
    {
        Code = $@"namespace {Namespace};

public class {Name}
{{
    public bool Success {{ get; set; }}
    public bool ErrorPasswordsDoNotMatch {{ get; set; }}
    public bool ErrorLockedOut {{ get; set; }}
}}";
        Save(false);
    }
}