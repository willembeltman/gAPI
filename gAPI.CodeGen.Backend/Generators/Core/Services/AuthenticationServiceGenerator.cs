using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Entities;

namespace gAPI.CodeGen.Backend.Generators.Core.Services;

public class AuthenticationServiceGenerator : BaseGenerator
{
    public AuthenticationServiceGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Core_ServicesDirectory;
        Namespace = context.Config.Core_ServicesNamespace;

        Context = context;

        Name = "AuthenticationService";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference ApplicationDbContext => Context.DbContext;
    public SharedReference IServerAuthenticationSecurity => Context.IServerAuthenticationSecurity;
    public SharedReference IServerAuthenticationService => Context.IServerAuthenticationService;
    public SharedReference LoginResponse => Context.LoginResponse;
    public SharedReference LoginRequest => Context.LoginRequest;
    public SharedReference RegisterResponse => Context.RegisterResponse;
    public SharedReference RegisterRequest => Context.RegisterRequest;
    public SharedReference ForgotPasswordResponse => Context.ForgotPasswordResponse;
    public SharedReference ForgotPasswordRequest => Context.ForgotPasswordRequest;
    public SharedReference IAuthenticationService => Context.IAuthenticationService;
    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;
    public SharedReference StringExtensions => Context.SharedReferences.StringExtensions;
    public SharedReference IsAuthorized => Context.SharedReferences.IsAuthorized;
    public SharedReference User => Context.DbContext.UserEntity;

    public void GenerateCode()
    {
        Reg("Microsoft.EntityFrameworkCore");
        Reg(ApplicationDbContext);
        Reg(IServerAuthenticationSecurity);
        Reg(IServerAuthenticationService);
        Reg(LoginResponse);
        Reg(LoginRequest);
        Reg(RegisterResponse);
        Reg(RegisterRequest);
        Reg(ForgotPasswordResponse);
        Reg(ForgotPasswordRequest);
        Reg(IAuthenticationService);
        Reg(BaseResponseT);
        Reg(StringExtensions);
        Reg(IsAuthorized);
        Reg(User);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}(
    {ApplicationDbContext} db,
    {IServerAuthenticationSecurity} securty,
    {IServerAuthenticationService} serverAuthenticationService)
    : {IAuthenticationService}
{{
    public async Task<{BaseResponseT}<{LoginResponse}>> Login({LoginRequest} request, CancellationToken ct)
    {{
        var result = await LoginAsync(
            request.Email,
            request.Password,
            ct);

        return new {BaseResponseT}<{LoginResponse}>()
        {{
            Success = result.Success,
            Response = result
        }};
    }}
    public async Task<{LoginResponse}> LoginAsync(string email, string password, CancellationToken ct)
    {{
        var allowedBefore = await securty.BeforeLoginAsync(ct);
        if (!allowedBefore)
            return new {LoginResponse}();

        var dbUser = await db.Users
            // Todo: Add includes for state if you use this
            .FirstOrDefaultAsync(a => a.Email == email, ct);
        if (dbUser == null)
            return new {LoginResponse}();

        var passwordHash = {StringExtensions}.HashString(password);
        if (dbUser.PasswordHash != passwordHash)
            return new {LoginResponse}()
            {{
                ErrorLockedOut = await securty.AfterUnSuccesfullLoginAsync(ct)
            }};

        var authenticationState = await serverAuthenticationService.AuthenticateUserAsync(dbUser, ct);
        if (authenticationState?.User == null)
            return new {LoginResponse}();

        var allowedAfter = await securty.AfterSuccesfullLoginAsync(ct);
        if (!allowedAfter)
            return new {LoginResponse}()
            {{
                ErrorLockedOut = true
            }};

        return new {LoginResponse}()
        {{
            Success = true
        }};
    }}

    public async Task<{BaseResponseT}<{RegisterResponse}>> Register({RegisterRequest} request, CancellationToken ct)
    {{
        var result = await RegisterAsync(
            request.UserName,
            request.Email,
            request.Password,
            request.PasswordAgain,
            ct);

        return new {BaseResponseT}<{RegisterResponse}>()
        {{
            Success = result.Success,
            Response = result,
        }};
    }}
    public async Task<{RegisterResponse}> RegisterAsync(string? userName, string? email, string? password, string? passwordRepeat, CancellationToken ct)
    {{
        var usernameLower = userName?.ToLower();
        var emailLower = email?.ToLower();

        if (string.IsNullOrWhiteSpace(email))
            return new {RegisterResponse}()
            {{
                ErrorEmailEmpty = true
            }};

        if (string.IsNullOrWhiteSpace(userName))
            return new {RegisterResponse}()
            {{
                ErrorUsernameEmpty = true
            }};

        if (string.IsNullOrWhiteSpace(password))
            return new {RegisterResponse}()
            {{
                ErrorPasswordEmpty = true
            }};

        if (password != passwordRepeat)
            return new {RegisterResponse}()
            {{
                ErrorPasswordsDoNotMatch = true
            }};

        var usernameUser = await db.Users.FirstOrDefaultAsync(a => a.UserName.ToLower() == usernameLower, ct);
        if (usernameUser != null)
            return new {RegisterResponse}()
            {{
                ErrorUsernameInUse = true
            }};

        var emailUser = await db.Users.FirstOrDefaultAsync(a => a.Email.ToLower() == emailLower, ct);
        if (emailUser != null)
            return new {RegisterResponse}()
            {{
                ErrorEmailInUse = true
            }};

        var allowed = await securty.BeforeRegisterAsync(ct);
        if (allowed == false)
            return new {RegisterResponse}()
            {{
                LockedOut = true
            }};

        var passwordHash = {StringExtensions}.HashString(password);
        var dbUser = new {User}(userName, email, passwordHash);
        await db.Users.AddAsync(dbUser, ct);
        await db.SaveChangesAsync(ct);

        var authenticationState = await serverAuthenticationService.AuthenticateUserAsync(dbUser, ct);
        if (authenticationState == null || authenticationState.User == null)
            return new {RegisterResponse}()
            {{
                ErrorCouldNotAuthenticateUser = true
            }};

        return new {RegisterResponse}()
        {{
            Success = true
        }};
    }}

    public async Task<{BaseResponseT}<{ForgotPasswordResponse}>> ForgotPassword({ForgotPasswordRequest} request, CancellationToken ct)
    {{
        var result = await ForgotPasswordAsync(
            request.Email,
            ct);

        return new {BaseResponseT}<{ForgotPasswordResponse}>()
        {{
            Success = result.Success,
            Response = result
        }};
    }}
    public async Task<{ForgotPasswordResponse}> ForgotPasswordAsync(string? email, CancellationToken ct)
    {{
        var emailLowered = email?.ToLower();

        var allowed = await securty.BeforeForgetPasswordAsync(ct);
        if (allowed == false)
            return new {ForgotPasswordResponse}()
            {{
                ErrorLockedOut = true
            }};

        var dbuser = await db.Users.FirstOrDefaultAsync(a => a.Email.ToLower() == emailLowered, ct);
        if (dbuser == null)
        {{
            // Todo implement this function for forgot password flow
        }}

        return new ForgotPasswordResponse()
        {{
            Success = true
        }};
    }}

    [{IsAuthorized}]
    public async Task<{BaseResponseT}<bool>> Logoff(CancellationToken ct)
    {{
        await serverAuthenticationService.LogoutAsync(ct);
        return new {BaseResponseT}<bool>()
        {{
            Success = true,
            Response = true
        }};
    }}
}}";
        Save(false);
    }
}