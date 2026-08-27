using gAPI.Core.Dtos;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace gAPI.Core.Server.Authentication;

public class UserTokenFactory<TUser>(
    IDbContextFactory<AuthenticationDbContext<TUser>> dbFactory)
    : IUserTokenFactory<TUser>
    where TUser : AuthUser
{
    public async Task<UserToken<TUser>> SaveTokenAsync(string userId, string cookieHash, CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        // Add new token hash to database
        var dbToken = new UserToken<TUser>()
        {
            UserId = Guid.Parse(userId),
            TokenHash = cookieHash
        };
        await db.Tokens.AddAsync(dbToken, ct);
        await db.SaveChangesAsync(ct);
        return dbToken;
    }
}