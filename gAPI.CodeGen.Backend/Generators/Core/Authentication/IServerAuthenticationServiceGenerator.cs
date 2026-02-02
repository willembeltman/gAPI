using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

public class IServerAuthenticationServiceGenerator : BaseGenerator
{
    public IServerAuthenticationServiceGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Core_AuthenticationDirectory;
        Namespace = context.Config.Core_AuthenticationNamespace;

        Context = context;

        Name = "IServerAuthenticationService";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public Entity User => Context.DbContext.UserEntity;
    public SharedReference ServerAuthenticationState => Context.ServerAuthenticationState;
    public SharedReference GapiIServerAuthenticationService => Context.SharedReferences.GapiIServerAuthenticationService;
    public SharedReference State => Context.State;

    public void GenerateCode()
    {
        Reg(User);
        Reg(State);
        Reg(ServerAuthenticationState);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public interface {Name} : {GapiIServerAuthenticationService.FullName}
{{
    {State}? ClientState {{ get; }}
    {ServerAuthenticationState} State {{ get; }}
    bool Initialized {{ get; }}

    Task<{ServerAuthenticationState}> AuthenticateUserAsync({User} dbUser, CancellationToken ct);
    Task LogoutAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}}";

        Save(false);
    }
}