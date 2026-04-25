using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server;

public class AuthenticationState<TUser>
    where TUser : AuthUser
{
    public AuthenticationState(TUser? dbUser, UserToken<TUser>? dbToken, Ip<TUser> dbIp)
    {
        User = dbUser;
        Token = dbToken;
        Ip = dbIp;
    }

    public TUser? User { get; }
    public UserToken<TUser>? Token { get; }
    public Ip<TUser> Ip { get; }
}