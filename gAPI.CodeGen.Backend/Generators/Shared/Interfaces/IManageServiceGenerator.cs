using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Shared.Interfaces;

public class IManageServiceGenerator : BaseGenerator
{
    public IManageServiceGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Shared_InterfacesDirectory;
        Namespace = context.Config.Shared_InterfacesNamespace;

        Context = context;

        Name = "IManageService";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference GenerateApi => Context.SharedReferences.GenerateApi;
    public SharedReference IsPage => Context.SharedReferences.IsPage;
    public SharedReference IsAuthorized => Context.SharedReferences.IsAuthorized;
    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;
    public SharedReference ChangePasswordResponse => Context.ChangePasswordResponse;
    public SharedReference ChangePasswordRequest => Context.ChangePasswordRequest;

    public void GenerateCode()
    {
        Reg(GenerateApi);
        Reg(IsPage);
        Reg(IsAuthorized);
        Reg(BaseResponseT);
        Reg(ChangePasswordResponse);
        Reg(ChangePasswordRequest);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

[{GenerateApi}]
[{IsAuthorized}]
public interface {Name}
{{
    [{IsPage}(""manage/changepassword"")]
    Task<{BaseResponseT}<{ChangePasswordResponse}>> ChangePassword({ChangePasswordRequest} request, CancellationToken ct);
}}";
        Save(false);
    }
}