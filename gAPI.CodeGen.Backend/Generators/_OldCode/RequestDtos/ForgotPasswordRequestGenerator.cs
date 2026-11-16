//namespace gAPI.CodeGen.Backend.Generators.Shared.RequestDtos;

//public class ForgotPasswordRequestGenerator : BaseGenerator
//{
//    public ForgotPasswordRequestGenerator(BackendGenerator context)
//    {
//        Directory = context.Config.Shared_RequestDtosDirectory;
//        Namespace = context.Config.Shared_RequestDtosNamespace;

//        Context = context;

//        Name = $"ForgotPasswordRequest";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public void GenerateCode()
//    {
//        Code = $@"namespace {Namespace};

//public class {Name}
//{{
//    public string? Email {{ get; set; }}
//}}";
//        Save(false);
//    }
//}