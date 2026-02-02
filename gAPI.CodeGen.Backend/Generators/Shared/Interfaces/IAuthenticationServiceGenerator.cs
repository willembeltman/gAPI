using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Shared.Interfaces;

public class IAuthenticationServiceGenerator : BaseGenerator
{
    public IAuthenticationServiceGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Shared_InterfacesDirectory;
        Namespace = context.Config.Shared_InterfacesNamespace;

        Context = context;

        Name = "IAuthenticationService";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference GenerateApi => Context.SharedReferences.GenerateApi;
    public SharedReference IsPage => Context.SharedReferences.IsPage;
    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;
    public SharedReference IsAuthorized => Context.SharedReferences.IsAuthorized;
    public SharedReference IsNotAuthorized => Context.SharedReferences.IsNotAuthorized;
    public SharedReference LoginResponse => Context.LoginResponse;
    public SharedReference LoginRequest => Context.LoginRequest;
    public SharedReference RegisterResponse => Context.RegisterResponse;
    public SharedReference RegisterRequest => Context.RegisterRequest;
    public SharedReference ForgotPasswordResponse => Context.ForgotPasswordResponse;
    public SharedReference ForgotPasswordRequest => Context.ForgotPasswordRequest;

    public void GenerateCode()
    {
        Reg(GenerateApi);
        Reg(IsPage);
        Reg(BaseResponseT);
        Reg(IsAuthorized);
        Reg(IsNotAuthorized);
        Reg(LoginResponse);
        Reg(LoginRequest);
        Reg(RegisterResponse);
        Reg(RegisterRequest);
        Reg(ForgotPasswordResponse);
        Reg(ForgotPasswordRequest);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[{GenerateApi}(""Auth"")]
public interface {Name}
{{
    [{IsPage}(""Account/Login""), {IsNotAuthorized}]
    Task<{BaseResponseT}<{LoginResponse}>> Login({LoginRequest} request, CancellationToken ct);

    [{IsPage}(""Account/Register""), {IsNotAuthorized}]
    Task<{BaseResponseT}<{RegisterResponse}>> Register({RegisterRequest} request, CancellationToken ct);

    [{IsPage}(""Account/ForgotPassword""), {IsNotAuthorized}]
    Task<{BaseResponseT}<{ForgotPasswordResponse}>> ForgotPassword({ForgotPasswordRequest} request, CancellationToken ct);

    [{IsPage}(""Account/Logoff""), {IsAuthorized}]
    Task<{BaseResponseT}<bool>> Logoff(CancellationToken ct);
}}";
        Save(false);
    }
}