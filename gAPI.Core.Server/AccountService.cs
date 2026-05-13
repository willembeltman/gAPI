using gAPI.Core.Dtos;
using gAPI.Core.Enums;
using gAPI.Core.Extensions;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace gAPI.Core.Server;

public class AccountService<TUser, TStateDto>(
    IDbContextFactory<AuthenticationDbContext<TUser>> dbFactory,
    IAuthenticationSecurity security,
    IAuthenticationService<TUser, TStateDto> authenticationService)
    : IAccountService
    where TUser : AuthUser, new()
    where TStateDto : AuthStateDto
{
    public async Task<BaseResponse> LoginAsync(string email, string password, CancellationToken ct)
    {
        var allowedBefore = await security.BeforeLoginAsync(ct);
        if (!allowedBefore)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorLockedOut
            };

        var db = dbFactory.CreateDbContext();
        var dbUser = await db.Users
            // Todo: Add includes for state if you use this
            .FirstOrDefaultAsync(a => a.Email == email, ct);
        if (dbUser == null)
            return new BaseResponse();

        var passwordHash = StringHelper.HashString(password);
        if (dbUser.PasswordHash != passwordHash)
            if (await security.AfterUnSuccesfullLoginAsync(ct))
            {
                return new BaseResponse()
                {
                    Error = BaseResponseErrorEnum.ErrorLockedOut
                };
            }
            else
            {
                return new BaseResponse();
            }

        var result = await authenticationService.AuthenticateUserAsync(dbUser.Id.ToString(), ct);
        if (result == false)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorCouldNotAuthenticateUser
            };

        var allowedAfter = await security.AfterSuccesfullLoginAsync(ct);
        if (!allowedAfter)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorLockedOut
            };

        authenticationService.State.ForceReconnect = true;

        // Return updated state
        return new BaseResponse()
        {
            Success = true,
            RedirectPath = "/"
        };
    }
    public async Task<BaseResponse> LogoffAsync(CancellationToken ct)
    {
        var result = await authenticationService.LogoffAsync(ct);

        authenticationService.State.ForceReconnect = true;

        return new BaseResponse()
        {
            Success = result,
            RedirectPath = "/"
        };
    }
    public async Task<BaseResponse> RegisterAsync(string userName, string email, string password, string passwordRepeat, CancellationToken ct)
    {
        var usernameLower = userName?.ToLower();
        var emailLower = email?.ToLower();

        if (string.IsNullOrWhiteSpace(email))
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorEmailEmpty
            };

        if (string.IsNullOrWhiteSpace(userName))
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorUsernameEmpty
            };

        if (string.IsNullOrWhiteSpace(password))
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorPasswordEmpty
            };

        if (password != passwordRepeat)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorPasswordsDoNotMatch
            };

        var db = dbFactory.CreateDbContext();

        // Leuke feature totdat ze bugs introduceren
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
        var usernameUser = await db.Users.FirstOrDefaultAsync(a => a.UserName.ToLower() == usernameLower, ct);
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

        if (usernameUser != null)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorUsernameInUse
            };

        // Leuke feature totdat ze bugs introduceren
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
        var emailUser = await db.Users.FirstOrDefaultAsync(a => a.Email.ToLower() == emailLower, ct);
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

        if (emailUser != null)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorEmailInUse
            };

        var allowed = await security.BeforeRegisterAsync(ct);
        if (allowed == false)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorLockedOut
            };

        var passwordHash = StringHelper.HashString(password);
        var dbUser = new TUser()
        {
            UserName = userName,
            Email = email,
            PasswordHash = passwordHash
        };
        await db.Users.AddAsync(dbUser, ct);
        await db.SaveChangesAsync(ct);

        var result = await authenticationService.AuthenticateUserAsync(dbUser.Id.ToString(), ct);
        if (result == false)
            return new BaseResponse()
            {
                Error = BaseResponseErrorEnum.ErrorCouldNotAuthenticateUser
            };

        authenticationService.State.ForceReconnect = true;

        return new BaseResponse()
        {
            Success = true,
            RedirectPath = "/"
        };
    }
}