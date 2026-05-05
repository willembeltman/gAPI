using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Mappings;
using gAPI.Core.Server.Dtos;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using gAPI.Core.Dtos;

namespace gAPI.Core.Server;

public class AuthenticationStateFactory<TUser, TStateDto>(
    IDbContextFactory<AuthenticationDbContext<TUser>> dbFactory,
    IStateMapping<TUser, TStateDto> stateMapping,
    ServerConfig config,
    TimeProvider dateTime,
    ILoggerFactory loggerFactory)
    : IAuthenticationStateFactory<TUser, TStateDto>
    where TUser : AuthUser
    where TStateDto : AuthStateDto, new()
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<AuthenticationStateFactory<TUser, TStateDto>>();

    public async Task SaveTokenAsync(string userId, string cookieHash, CancellationToken ct)
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
    }

    public async Task<(TStateDto, AuthenticationState<TUser>)> CreateAuthenticationStateAsync(
        AuthenticationHeaders headers,
        TStateDto? receivedClientState, // <-- IMPORTANT: DO NOT TRUST THIS STATE
        CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        var shortago = dateTime.GetUtcNow().AddHours(config.ShortHoursAgo);
        var longago = dateTime.GetUtcNow().AddHours(config.LongHoursAgo);

        UserToken<TUser>? dbToken = null;
        TUser? dbUser = null;

        if (headers.CookieHash != null)
        {
            dbToken = await db.Tokens
                .OrderByDescending(a => a.Date)
                .FirstOrDefaultAsync(a =>
                    a.TokenHash == headers.CookieHash &&
                    a.Date > longago,
                    ct);
        }

        if (dbToken != null &&
            dbToken.Date > longago)
        {
            dbUser = await db.Users
                .FirstOrDefaultAsync(a =>
                    a.Id == dbToken.UserId,
                    ct);
            if (dbUser != null)
            {
                if (dbToken.Date < shortago)
                {
                    var cookieHash = headers.CreateNewCookie();
                    dbToken = new UserToken<TUser>(dbUser.Id, cookieHash);
                    await db.Tokens.AddAsync(dbToken, ct);
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        var userId = dbUser?.Id;
        var authenticationTokenId = dbToken?.Id;

        var dbIp = await db.Ips
            .FirstOrDefaultAsync(a =>
                a.Address == headers.IpAdress.ToString(),
                ct);
        if (dbIp == null)
        {
            dbIp = new Ip<TUser>(headers.IpAdress.ToString());
            await db.Ips.AddAsync(dbIp, ct);
            await db.SaveChangesAsync(ct);
        }

        _ = Task.Run(async () => await DoTheRest(headers, userId, authenticationTokenId, dbUser, dbToken, dbIp, ct));

        var state = await stateMapping.ToDtoAsync(
            dbUser,
            dbToken,
            dbIp,
            receivedClientState,
            ct);

        var authState = new AuthenticationState<TUser>(
            dbUser,
            dbToken,
            dbIp);

        return (state, authState);
    }

    public async Task<RequestIds> DoTheRest(
        AuthenticationHeaders headers,
        Guid? userId, long? authenticationTokenId,
        TUser? dbUser, UserToken<TUser>? dbToken, Ip<TUser> dbIp,
        CancellationToken ct)
    {
        if (config.UseMemoryDatabase)
        {
            return await DoTheRest_EFCore(headers, userId, authenticationTokenId, dbUser, dbToken, dbIp, ct);
        }
        return await DoTheRest_StoredProcedure(dbUser?.Id, dbToken?.Id, dbIp.Id, headers.SessionId.Value, headers.Path, ct);
    }

    public async Task<RequestIds> DoTheRest_StoredProcedure(
        Guid? userId, long? tokenId, long ipId, string sessionCode, string routePath, CancellationToken ct)
    {
        var sql = @"
        DECLARE @SessionCode NVARCHAR(256) = @pSessionCode;
        DECLARE @RoutePath NVARCHAR(256) = @pRoutePath;

        DECLARE @UserId UNIQUEIDENTIFIER = @pUserId;
        DECLARE @TokenId BIGINT = @pTokenId;
        DECLARE @IpId BIGINT = @pIpId;
        DECLARE @Now DATETIMEOFFSET = @pNow;

        DECLARE @SessionId BIGINT;
        DECLARE @RouteId BIGINT;
        DECLARE @UserIpId BIGINT;
        DECLARE @UserIpSessionId BIGINT;
        DECLARE @UserIpSessionTokenId BIGINT;
        DECLARE @UserIpSessionTokenRouteId BIGINT;
        DECLARE @UserIpSessionTokenRouteRequestId BIGINT;

        DECLARE @Counter INT

        SET @Counter = 0;

        -- =========================
        -- Session
        -- =========================
        SELECT @SessionId = Id
        FROM Sessions
        WHERE SessionId = @SessionCode;

        IF (@SessionId IS NULL)
        BEGIN
            INSERT INTO Sessions (SessionId)
            VALUES (@SessionCode);

            SET @SessionId = SCOPE_IDENTITY();
        END       

        -- =========================
        -- Route
        -- =========================
        SELECT @RouteId = Id
        FROM Routes
        WHERE RouteName = @RoutePath;

        IF (@RouteId IS NULL)
        BEGIN
            INSERT INTO Routes (RouteName)
            VALUES (@RoutePath);

            SET @RouteId = SCOPE_IDENTITY();
        END

        -- =========================
        -- UserIp
        -- =========================
        SELECT @UserIpId = Id
        FROM UserIps
        WHERE UserId = @UserId
          AND IpId = @IpId;

        IF (@UserIpId IS NULL)
        BEGIN
            INSERT INTO UserIps (UserId, IpId)
            VALUES (@UserId, @IpId);

            SET @UserIpId = SCOPE_IDENTITY();
        END

        -- =========================
        -- UserIpSession
        -- =========================
        SELECT @UserIpSessionId = Id
        FROM UserIpSessions
        WHERE UserIpId = @UserIpId
          AND SessionId = @SessionId;

        IF (@UserIpSessionId IS NULL)
        BEGIN
            INSERT INTO UserIpSessions (UserIpId, SessionId)
            VALUES (@UserIpId, @SessionId);

            SET @UserIpSessionId = SCOPE_IDENTITY();
        END

        -- =========================
        -- UserIpSessionToken
        -- =========================
        SELECT @UserIpSessionTokenId = Id
        FROM UserIpSessionTokens
        WHERE UserIpSessionId = @UserIpSessionId
          AND TokenId = @TokenId;

        IF (@UserIpSessionTokenId IS NULL)
        BEGIN
            INSERT INTO UserIpSessionTokens
                (UserIpSessionId, TokenId)
            VALUES
                (@UserIpSessionId, @TokenId);

            SET @UserIpSessionTokenId = SCOPE_IDENTITY();
        END

        -- =========================
        -- UserIpSessionTokenRoute
        -- =========================
        SELECT @UserIpSessionTokenRouteId = Id
        FROM UserIpSessionTokenRoutes
        WHERE UserIpSessionTokenId = @UserIpSessionTokenId
          AND RouteId = @RouteId;

        IF (@UserIpSessionTokenRouteId IS NULL)
        BEGIN
            INSERT INTO UserIpSessionTokenRoutes
                (UserIpSessionTokenId, RouteId)
            VALUES
                (@UserIpSessionTokenId, @RouteId);

            SET @UserIpSessionTokenRouteId = SCOPE_IDENTITY();
        END

        -- =========================
        -- UserIpSessionTokenRouteRequest (ALTIJD NIEUW)
        -- =========================
        SELECT @UserIpSessionTokenRouteRequestId = Id, @Counter = [Count]
        FROM UserIpSessionTokenRouteRequests
        WHERE UserIpSessionTokenRouteId = @UserIpSessionTokenRouteId
        AND [Year] = YEAR(@Now)
        AND [Month] = MONTH(@Now)
        AND [Day] = DAY(@Now)
        AND [Hour] = DATEPART(HOUR, @Now);

        IF (@UserIpSessionTokenRouteRequestId IS NULL)
        BEGIN
            INSERT INTO UserIpSessionTokenRouteRequests
                (UserIpSessionTokenRouteId, [Year], [Month], [Day], [Hour], [Count])
            VALUES
                (@UserIpSessionTokenRouteId, YEAR(@Now), MONTH(@Now), DAY(@Now), DATEPART(HOUR, @Now), 1);

            SET @UserIpSessionTokenRouteRequestId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE UserIpSessionTokenRouteRequests
            SET 
                [Count] = @Counter + 1
            WHERE Id = @UserIpSessionTokenRouteRequestId;        
        END        
        
        SELECT
            @SessionId AS SessionId,
            @RouteId AS RouteId,
            @UserIpId AS UserIpId,
            @UserIpSessionId AS UserIpSessionId,
            @UserIpSessionTokenId AS UserIpSessionTokenId,
            @UserIpSessionTokenRouteId AS UserIpSessionTokenRouteId,
            @UserIpSessionTokenRouteRequestId AS UserIpSessionTokenRouteRequestId,
            @Counter + 1 AS Counter;

        ";

        var parameters = new SqlParameter[]
        {
            new("@pSessionCode", SqlDbType.NVarChar) { Value = sessionCode },
            new("@pRoutePath", SqlDbType.NVarChar) { Value = routePath },

            new("@pUserId", SqlDbType.UniqueIdentifier) { Value = (object?)userId ?? DBNull.Value },
            new("@pTokenId", SqlDbType.BigInt) { Value = (object?)tokenId ?? DBNull.Value },
            new("@pIpId", SqlDbType.BigInt) { Value = ipId },
            new("@pNow", SqlDbType.DateTimeOffset) { Value = dateTime.GetUtcNow() }
        };

        var db = await dbFactory.CreateDbContextAsync(ct);
        var result = db.Database
            .SqlQueryRaw<RequestIds>(sql, parameters)
            .AsEnumerable()
            .Single();

        return result;
    }

    public async Task<RequestIds> DoTheRest_EFCore(
        AuthenticationHeaders headers,
        Guid? userId, long? authenticationTokenId,
        TUser? dbUser, UserToken<TUser>? dbToken, Ip<TUser> dbIp,
        CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        var dbSession = await db.Sessions
            .FirstOrDefaultAsync(a =>
                a.SessionId == headers.SessionId.Value,
                ct);
        if (dbSession == null)
        {
            dbSession = new Session<TUser>(headers.SessionId.Value);
            await db.Sessions.AddAsync(dbSession, ct);
            await db.SaveChangesAsync(ct); // Gaat goed
        }

        var dbRoute = await db.Routes
            .FirstOrDefaultAsync(a =>
                a.RouteName == headers.EncodedPath,
                ct);
        if (dbRoute == null)
        {
            dbRoute = new Route<TUser>(headers.EncodedPath);
            await db.Routes.AddAsync(dbRoute, ct);
            await db.SaveChangesAsync(ct); // Gaat goed
        }

        var dbUserIp = await db.UserIps
            .FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.IpId == dbIp.Id,
                ct);
        if (dbUserIp == null)
        {
            dbUserIp = new UserIp<TUser>(userId, dbIp); // userId = null
            await db.UserIps.AddAsync(dbUserIp, ct);
            await db.SaveChangesAsync(ct); // Gaat fout: error kan `iets` niet toevoegen dat al bestaat.
        }

        var dbUserIpSession = await db.UserIpSessions
                .FirstOrDefaultAsync(a =>
                    a.UserIpId == dbUserIp.Id &&
                    a.SessionId == dbSession.Id,
                    ct);
        if (dbUserIpSession == null)
        {
            dbUserIpSession = new UserIpSession<TUser>(dbUserIp, dbSession);
            await db.UserIpSessions.AddAsync(dbUserIpSession, ct);
            await db.SaveChangesAsync(ct);
        }

        var dbUserIpSessionToken = await db.UserIpSessionTokens
            .FirstOrDefaultAsync(a =>
                a.TokenId == authenticationTokenId &&
                a.UserIpSessionId == dbUserIpSession.Id,
                ct);
        if (dbUserIpSessionToken == null)
        {
            dbUserIpSessionToken = new UserIpSessionToken<TUser>(dbUserIpSession, dbToken);
            await db.UserIpSessionTokens.AddAsync(dbUserIpSessionToken, ct);
            await db.SaveChangesAsync(ct);
        }

        var dbUserIpSessionTokenRoute = await db.UserIpSessionTokenRoutes
            .FirstOrDefaultAsync(a =>
                a.RouteId == dbRoute.Id &&
                a.UserIpSessionTokenId == dbUserIpSessionToken.Id,
                ct);
        if (dbUserIpSessionTokenRoute == null)
        {
            dbUserIpSessionTokenRoute = new UserIpSessionTokenRoute<TUser>(dbUserIpSessionToken, dbRoute);
            await db.UserIpSessionTokenRoutes.AddAsync(dbUserIpSessionTokenRoute, ct);
            await db.SaveChangesAsync(ct);
        }

        var dbUserIpSessionTokenRouteRequest = new UserIpSessionTokenRouteRequest<TUser>(dbUserIpSessionTokenRoute, dateTime.GetUtcNow());
        await db.UserIpSessionTokenRouteRequests.AddAsync(dbUserIpSessionTokenRouteRequest, ct);
        await db.SaveChangesAsync(ct);

        return new RequestIds()
        {
            Counter = 1,
            RouteId = dbRoute.Id,
            SessionId = dbSession.Id,
            UserIpId = dbUserIp.Id,
            UserIpSessionId = dbUserIpSession.Id,
            UserIpSessionTokenId = dbUserIpSessionToken.Id,
            UserIpSessionTokenRouteId = dbUserIpSessionTokenRoute.Id,
            UserIpSessionTokenRouteRequestId = dbUserIpSessionTokenRouteRequest.Id,
        };
    }
}