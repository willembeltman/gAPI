//namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

//public class IServerAuthenticationSecurityGenerator : BaseGenerator
//{
//    public IServerAuthenticationSecurityGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Core_AuthenticationDirectory;
//        Namespace = context.Config.Core_AuthenticationNamespace;

//        Context = context;

//        Name = "IServerAuthenticationSecurity";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public void GenerateCode()
//    {
//        Code = @$"namespace {Namespace};

//public interface {Name}
//{{
//    Task<bool> AfterSuccesfullChangePasswordAsync(CancellationToken ct);
//    Task<bool> AfterSuccesfullLoginAsync(CancellationToken ct);
//    Task<bool> AfterUnSuccesfullChangePasswordAsync(CancellationToken ct);
//    Task<bool> AfterUnSuccesfullLoginAsync(CancellationToken ct);
//    Task<bool> BeforeChangePasswordAsync(CancellationToken ct);
//    Task<bool> BeforeForgetPasswordAsync(CancellationToken ct);
//    Task<bool> BeforeLoginAsync(CancellationToken ct);
//    Task<bool> BeforeRegisterAsync(CancellationToken ct);
//}}";
//        Save(false);
//    }
//}