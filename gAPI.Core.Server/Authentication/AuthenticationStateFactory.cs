using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationStateFactory<TUser>(
    IDbContextFactory<AuthenticationDbContext<TUser>> dbFactory,
    TimeProvider dateTime,
    double ShortHoursAgo,
    double LongHoursAgo,
    bool UseMemoryDatabase)
    : IAuthenticationStateFactory<TUser>
    where TUser : AuthUser
{
    public async Task<AuthenticationState<TUser>> CreateAuthenticationStateAsync(
        AuthenticationHeaders headers,
        CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        var shortago = dateTime.GetUtcNow().AddHours(ShortHoursAgo);
        var longago = dateTime.GetUtcNow().AddHours(LongHoursAgo);

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

        var authState = new AuthenticationState<TUser>(
            dbUser,
            dbToken,
            dbIp);

        return authState;
    }

    public async Task<RequestIds> DoTheRest(
        AuthenticationHeaders headers,
        Guid? userId, long? authenticationTokenId,
        TUser? dbUser, UserToken<TUser>? dbToken, Ip<TUser> dbIp,
        CancellationToken ct)
    {
        if (UseMemoryDatabase)
        {
            return await DoTheRest_EFCore(headers, userId, authenticationTokenId, dbUser, dbToken, dbIp, ct);
        }
        return await DoTheRest_StoredProcedure(dbUser?.Id, dbToken?.Id, dbIp.Id, headers.SessionId.Value, headers.Path, ct);
    }

    public async Task<RequestIds> DoTheRest_StoredProcedure(
        Guid? userId, long? tokenId, long ipId, string sessionCode, string routePath, CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        var now = dateTime.GetUtcNow().UtcDateTime;

        // Deze query is 100% geldig in Postgres en voert alles in één transactie/roundtrip uit
        var sql = @"
    WITH 
    ins_session AS (
        INSERT INTO ""Sessions"" (""SessionId"") VALUES (@pSessionCode)
        ON CONFLICT (""SessionId"") DO UPDATE SET ""SessionId"" = EXCLUDED.""SessionId"" RETURNING ""Id""
    ),
    ins_route AS (
        INSERT INTO ""Routes"" (""RouteName"") VALUES (@pRoutePath)
        ON CONFLICT (""RouteName"") DO UPDATE SET ""RouteName"" = EXCLUDED.""RouteName"" RETURNING ""Id""
    ),
    ins_userip AS (
        INSERT INTO ""UserIps"" (""UserId"", ""IpId"") VALUES (@pUserId, @pIpId)
        ON CONFLICT (""UserId"", ""IpId"") DO UPDATE SET ""UserId"" = EXCLUDED.""UserId"" RETURNING ""Id""
    ),
    ins_useripsession AS (
        INSERT INTO ""UserIpSessions"" (""UserIpId"", ""SessionId"") 
        SELECT ins_userip.""Id"", ins_session.""Id"" FROM ins_userip, ins_session
        ON CONFLICT (""UserIpId"", ""SessionId"") DO UPDATE SET ""UserIpId"" = EXCLUDED.""UserIpId"" RETURNING ""Id""
    ),
    ins_useripsessiontoken AS (
        INSERT INTO ""UserIpSessionTokens"" (""UserIpSessionId"", ""TokenId"") 
        SELECT ins_useripsession.""Id"", @pTokenId FROM ins_useripsession
        ON CONFLICT (""UserIpSessionId"", ""TokenId"") DO UPDATE SET ""UserIpSessionId"" = EXCLUDED.""UserIpSessionId"" RETURNING ""Id""
    ),
    ins_useripsessiontokenroute AS (
        INSERT INTO ""UserIpSessionTokenRoutes"" (""UserIpSessionTokenId"", ""RouteId"") 
        SELECT ins_useripsessiontoken.""Id"", ins_route.""Id"" FROM ins_useripsessiontoken, ins_route
        ON CONFLICT (""UserIpSessionTokenId"", ""RouteId"") DO UPDATE SET ""UserIpSessionTokenId"" = EXCLUDED.""UserIpSessionTokenId"" RETURNING ""Id""
    ),
    ins_request AS (
        INSERT INTO ""UserIpSessionTokenRouteRequests"" (""UserIpSessionTokenRouteId"", ""Year"", ""Month"", ""Day"", ""Hour"", ""Count"")
        SELECT ins_useripsessiontokenroute.""Id"", EXTRACT(YEAR FROM @pNow)::int, EXTRACT(MONTH FROM @pNow)::int, EXTRACT(DAY FROM @pNow)::int, EXTRACT(HOUR FROM @pNow)::int, 1
        FROM ins_useripsessiontokenroute
        ON CONFLICT (""UserIpSessionTokenRouteId"", ""Year"", ""Month"", ""Day"", ""Hour"") 
        DO UPDATE SET ""Count"" = ""UserIpSessionTokenRouteRequests"".""Count"" + 1 RETURNING ""Id"", ""Count""
    )
    SELECT 
        (SELECT ""Id"" FROM ins_session) AS SessionId,
        (SELECT ""Id"" FROM ins_route) AS RouteId,
        (SELECT ""Id"" FROM ins_userip) AS UserIpId,
        (SELECT ""Id"" FROM ins_useripsession) AS UserIpSessionId,
        (SELECT ""Id"" FROM ins_useripsessiontoken) AS UserIpSessionTokenId,
        (SELECT ""Id"" FROM ins_useripsessiontokenroute) AS UserIpSessionTokenRouteId,
        (SELECT ""Id"" FROM ins_request) AS UserIpSessionTokenRouteRequestId,
        (SELECT ""Count"" FROM ins_request) AS Counter;";

        // We openen de connectie handmatig en maken een command aan om EF te passeren
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;

        // Parameters toevoegen
        cmd.Parameters.Add(new NpgsqlParameter("@pSessionCode", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = sessionCode });
        cmd.Parameters.Add(new NpgsqlParameter("@pRoutePath", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = routePath });
        cmd.Parameters.Add(new NpgsqlParameter("@pUserId", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = (object?)userId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("@pTokenId", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = (object?)tokenId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("@pIpId", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = ipId });
        cmd.Parameters.Add(new NpgsqlParameter("@pNow", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = now });

        // Voer uit en lees direct uit in één roundtrip
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new RequestIds
            {
                SessionId = reader.GetInt64(reader.GetOrdinal("SessionId")),
                RouteId = reader.GetInt64(reader.GetOrdinal("RouteId")),
                UserIpId = reader.GetInt64(reader.GetOrdinal("UserIpId")),
                UserIpSessionId = reader.GetInt64(reader.GetOrdinal("UserIpSessionId")),
                UserIpSessionTokenId = reader.GetInt64(reader.GetOrdinal("UserIpSessionTokenId")),
                UserIpSessionTokenRouteId = reader.GetInt64(reader.GetOrdinal("UserIpSessionTokenRouteId")),
                UserIpSessionTokenRouteRequestId = reader.GetInt64(reader.GetOrdinal("UserIpSessionTokenRouteRequestId")),
                Counter = reader.GetInt32(reader.GetOrdinal("Counter"))
            };
        }

        throw new InvalidOperationException("PostgreSQL keerde geen resultaat terug.");
    }


    //public async Task<RequestIds> DoTheRest_StoredProcedure(
    //    Guid? userId, long? tokenId, long ipId, string sessionCode, string routePath, CancellationToken ct)
    //{
    //    var sql = @"
    //    DECLARE @SessionCode NVARCHAR(256) = @pSessionCode;
    //    DECLARE @RoutePath NVARCHAR(256) = @pRoutePath;

    //    DECLARE @UserId UNIQUEIDENTIFIER = @pUserId;
    //    DECLARE @TokenId BIGINT = @pTokenId;
    //    DECLARE @IpId BIGINT = @pIpId;
    //    DECLARE @Now DATETIMEOFFSET = @pNow;

    //    DECLARE @SessionId BIGINT;
    //    DECLARE @RouteId BIGINT;
    //    DECLARE @UserIpId BIGINT;
    //    DECLARE @UserIpSessionId BIGINT;
    //    DECLARE @UserIpSessionTokenId BIGINT;
    //    DECLARE @UserIpSessionTokenRouteId BIGINT;
    //    DECLARE @UserIpSessionTokenRouteRequestId BIGINT;

    //    DECLARE @Counter INT

    //    SET @Counter = 0;

    //    -- =========================
    //    -- Session
    //    -- =========================
    //    SELECT @SessionId = Id
    //    FROM Sessions
    //    WHERE SessionId = @SessionCode;

    //    IF (@SessionId IS NULL)
    //    BEGIN
    //        INSERT INTO Sessions (SessionId)
    //        VALUES (@SessionCode);

    //        SET @SessionId = SCOPE_IDENTITY();
    //    END       

    //    -- =========================
    //    -- Route
    //    -- =========================
    //    SELECT @RouteId = Id
    //    FROM Routes
    //    WHERE RouteName = @RoutePath;

    //    IF (@RouteId IS NULL)
    //    BEGIN
    //        INSERT INTO Routes (RouteName)
    //        VALUES (@RoutePath);

    //        SET @RouteId = SCOPE_IDENTITY();
    //    END

    //    -- =========================
    //    -- UserIp
    //    -- =========================
    //    SELECT @UserIpId = Id
    //    FROM UserIps
    //    WHERE UserId = @UserId
    //      AND IpId = @IpId;

    //    IF (@UserIpId IS NULL)
    //    BEGIN
    //        INSERT INTO UserIps (UserId, IpId)
    //        VALUES (@UserId, @IpId);

    //        SET @UserIpId = SCOPE_IDENTITY();
    //    END

    //    -- =========================
    //    -- UserIpSession
    //    -- =========================
    //    SELECT @UserIpSessionId = Id
    //    FROM UserIpSessions
    //    WHERE UserIpId = @UserIpId
    //      AND SessionId = @SessionId;

    //    IF (@UserIpSessionId IS NULL)
    //    BEGIN
    //        INSERT INTO UserIpSessions (UserIpId, SessionId)
    //        VALUES (@UserIpId, @SessionId);

    //        SET @UserIpSessionId = SCOPE_IDENTITY();
    //    END

    //    -- =========================
    //    -- UserIpSessionToken
    //    -- =========================
    //    SELECT @UserIpSessionTokenId = Id
    //    FROM UserIpSessionTokens
    //    WHERE UserIpSessionId = @UserIpSessionId
    //      AND TokenId = @TokenId;

    //    IF (@UserIpSessionTokenId IS NULL)
    //    BEGIN
    //        INSERT INTO UserIpSessionTokens
    //            (UserIpSessionId, TokenId)
    //        VALUES
    //            (@UserIpSessionId, @TokenId);

    //        SET @UserIpSessionTokenId = SCOPE_IDENTITY();
    //    END

    //    -- =========================
    //    -- UserIpSessionTokenRoute
    //    -- =========================
    //    SELECT @UserIpSessionTokenRouteId = Id
    //    FROM UserIpSessionTokenRoutes
    //    WHERE UserIpSessionTokenId = @UserIpSessionTokenId
    //      AND RouteId = @RouteId;

    //    IF (@UserIpSessionTokenRouteId IS NULL)
    //    BEGIN
    //        INSERT INTO UserIpSessionTokenRoutes
    //            (UserIpSessionTokenId, RouteId)
    //        VALUES
    //            (@UserIpSessionTokenId, @RouteId);

    //        SET @UserIpSessionTokenRouteId = SCOPE_IDENTITY();
    //    END

    //    -- =========================
    //    -- UserIpSessionTokenRouteRequest (ALTIJD NIEUW)
    //    -- =========================
    //    SELECT @UserIpSessionTokenRouteRequestId = Id, @Counter = [Count]
    //    FROM UserIpSessionTokenRouteRequests
    //    WHERE UserIpSessionTokenRouteId = @UserIpSessionTokenRouteId
    //    AND [Year] = YEAR(@Now)
    //    AND [Month] = MONTH(@Now)
    //    AND [Day] = DAY(@Now)
    //    AND [Hour] = DATEPART(HOUR, @Now);

    //    IF (@UserIpSessionTokenRouteRequestId IS NULL)
    //    BEGIN
    //        INSERT INTO UserIpSessionTokenRouteRequests
    //            (UserIpSessionTokenRouteId, [Year], [Month], [Day], [Hour], [Count])
    //        VALUES
    //            (@UserIpSessionTokenRouteId, YEAR(@Now), MONTH(@Now), DAY(@Now), DATEPART(HOUR, @Now), 1);

    //        SET @UserIpSessionTokenRouteRequestId = SCOPE_IDENTITY();
    //    END
    //    ELSE
    //    BEGIN
    //        UPDATE UserIpSessionTokenRouteRequests
    //        SET 
    //            [Count] = @Counter + 1
    //        WHERE Id = @UserIpSessionTokenRouteRequestId;        
    //    END        

    //    SELECT
    //        @SessionId AS SessionId,
    //        @RouteId AS RouteId,
    //        @UserIpId AS UserIpId,
    //        @UserIpSessionId AS UserIpSessionId,
    //        @UserIpSessionTokenId AS UserIpSessionTokenId,
    //        @UserIpSessionTokenRouteId AS UserIpSessionTokenRouteId,
    //        @UserIpSessionTokenRouteRequestId AS UserIpSessionTokenRouteRequestId,
    //        @Counter + 1 AS Counter;

    //    ";

    //    var parameters = new SqlParameter[]
    //    {
    //        new("@pSessionCode", SqlDbType.NVarChar) { Value = sessionCode },
    //        new("@pRoutePath", SqlDbType.NVarChar) { Value = routePath },

    //        new("@pUserId", SqlDbType.UniqueIdentifier) { Value = (object?)userId ?? DBNull.Value },
    //        new("@pTokenId", SqlDbType.BigInt) { Value = (object?)tokenId ?? DBNull.Value },
    //        new("@pIpId", SqlDbType.BigInt) { Value = ipId },
    //        new("@pNow", SqlDbType.DateTimeOffset) { Value = dateTime.GetUtcNow() }
    //    };

    //    var db = await dbFactory.CreateDbContextAsync(ct);
    //    var result = db.Database
    //        .SqlQueryRaw<RequestIds>(sql, parameters)
    //        .AsEnumerable()
    //        .Single();

    //    return result;
    //}

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