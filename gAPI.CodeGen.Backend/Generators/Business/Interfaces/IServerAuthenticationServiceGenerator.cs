using gAPI.CodeGen.Backend.Generators.Business.Models;

namespace gAPI.CodeGen.Backend.Generators.Business.Interfaces;

public class IServerAuthenticationServiceGenerator : BaseGenerator
{
    public IServerAuthenticationServiceGenerator(BackendGenerator context)
    {
        Context = context;

        Directory = context.Config.BusinessInterfacesDirectory;
        Namespace = context.Config.BusinessInterfacesNamespace;

        Name = "IServerAuthenticationService";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }
    public AuthenticationStateGenerator AuthenticationState => Context.AuthenticationState;

    public void GenerateCode()
    {
        Reg(Context.DbContext.UserEntity);
        Reg(AuthenticationState);

        Code = $@"{GetNamespacesCode()}namespace {Namespace};

public interface {Name} : gAPI.Interfaces.IServerAuthenticationService
{{
    Task<{AuthenticationState.Name}> GetAuthenticationStateAsync();
    Task<{AuthenticationState.Name}> AuthenticateUserAsync({Context.DbContext.UserEntity.Name} dbUser);
    Task LogoutAsync();

    Task<bool> BeforeRegisterAsync();
    Task<bool> BeforeLoginAsync();
    Task<bool> AfterSuccesfullLoginAsync();
    Task<bool> AfterUnSuccesfullLoginAsync();
    Task<bool> BeforeForgetPasswordAsync();
    Task<bool> BeforeChangePasswordAsync();
    Task<bool> AfterSuccesfullChangePasswordAsync();
    Task<bool> AfterUnSuccesfullChangePasswordAsync();
}}";

        Save(false);
    }
}