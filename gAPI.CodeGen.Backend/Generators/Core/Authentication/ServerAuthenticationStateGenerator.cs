using gAPI.CodeGen.Backend.Generators.Data.Authentication;
using gAPI.CodeGen.Backend.Generators.Shared.StateDtos;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

public class ServerAuthenticationStateGenerator : BaseGenerator
{
    public ServerAuthenticationStateGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Core_AuthenticationDirectory;
        Namespace = context.Config.Core_AuthenticationNamespace;

        Context = context;

        Name = "ServerAuthenticationState";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference State => Context.State;
    public SharedReference StateUser => Context.State.User;
    public SharedReference Token => Context.Token;
    public SharedReference User => Context.DbContext.UserEntity;
    public SharedReference Ip => Context.Ip;

    public void GenerateCode()
    {
        Reg(State);
        Reg(StateUser);
        Reg(Token);
        Reg(User);
        Reg(Ip);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name} : {State}
{{
    public {Name}({StateUser}? user, {Token}? dbToken, {User}? dbUser, {Ip} dbIp)
    {{
        User = user;
        DbToken = dbToken;
        DbUser = dbUser;
        DbIp = dbIp;
    }}

    public {Token}? DbToken {{ get; }}
    public {User}? DbUser {{ get; }}
    public {Ip} DbIp {{ get; }}
}}";
        Save(false);
    }
}