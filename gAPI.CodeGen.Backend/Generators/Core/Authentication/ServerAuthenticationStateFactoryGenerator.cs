//using gAPI.CodeGen.Backend.Generators.Data.Authentication;
//using gAPI.CodeGen.Backend.Generators.Shared.StateDtos;
//using gAPI.CodeGen.Backend.Models;
//using gAPI.CodeGen.Backend.Models.Entities;

//namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

//public class ServerAuthenticationStateFactoryGenerator : BaseGenerator
//{
//    public ServerAuthenticationStateFactoryGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Core_AuthenticationDirectory;
//        Namespace = context.Config.Core_AuthenticationNamespace;

//        Context = context;

//        Name = "ServerAuthenticationStateFactory";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public DbContext ApplicationDbContext => Context.DbContext;
//    public StateMappingGenerator StateMapping => Context.StateMapping;
//    public SharedReference ServerConfig => Context.SharedReferences.ServerConfig;
//    public IServerAuthenticationStateFactoryGenerator IServerAuthenticationStateFactory => Context.IServerAuthenticationStateFactory;
//    public SharedReference AuthenticationHeaders => Context.SharedReferences.AuthenticationHeaders;
//    public TokenGenerator Token => Context.Token;
//    public Entity User => Context.DbContext.UserEntity;
//    public IpGenerator Ip => Context.Ip;
//    public ServerAuthenticationStateGenerator ServerAuthenticationState => Context.ServerAuthenticationState;
//    public StateDtoGenerator StateUser => Context.State.User;
//    public RequestIdsGenerator RequestIds => Context.RequestIds;
//    public SessionGenerator Session => Context.Session;
//    public RouteGenerator Route => Context.Route;
//    public UserIpGenerator UserIp => Context.UserIp;
//    public UserIpSessionGenerator UserIpSession => Context.UserIpSession;
//    public UserIpSessionTokenGenerator UserIpSessionToken => Context.UserIpSessionToken;
//    public UserIpSessionTokenRouteGenerator UserIpSessionTokenRoute => Context.UserIpSessionTokenRoute;
//    public UserIpSessionTokenRouteRequestGenerator UserIpSessionTokenRouteRequest => Context.UserIpSessionTokenRouteRequest;

//    public void GenerateCode()
//    {
//        Reg("Microsoft.Data.SqlClient");
//        Reg("Microsoft.EntityFrameworkCore");
//        Reg("Microsoft.Extensions.Logging");
//        Reg("System.Data");
//        Reg(ApplicationDbContext);
//        Reg(StateMapping);
//        Reg(ServerConfig);
//        Reg(IServerAuthenticationStateFactory);
//        Reg(Token);
//        Reg(User);
//        Reg(Ip);
//        Reg(ServerAuthenticationState);
//        Reg(StateUser);
//        Reg(RequestIds);
//        Reg(Session);
//        Reg(Route);
//        Reg(UserIp);
//        Reg(UserIpSession);
//        Reg(UserIpSessionToken);
//        Reg(UserIpSessionTokenRoute);
//        Reg(UserIpSessionTokenRouteRequest);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public class {Name}(
//    {ApplicationDbContext} db,
//    {StateMapping} mapping,
//    {ServerConfig} config,
//    TimeProvider dateTime,
//    ILoggerFactory loggerFactory) 
//    : {IServerAuthenticationStateFactory}
//{{
//    private readonly ILogger Logger = loggerFactory.CreateLogger<{Name}>();

//    public async Task<ServerAuthenticationState> CreateAuthenticationStateAsync(
//        {AuthenticationHeaders.FullName} headers,
//        State? receivedClientState, // <-- IMPORTANT: DO NOT TRUST THIS STATE
//        CancellationToken ct)
//    {{
//        var shortago = dateTime.GetUtcNow().AddHours(config.ShortHoursAgo);
//        var longago = dateTime.GetUtcNow().AddHours(config.LongHoursAgo);

//        {Token}? dbToken = null;
//        {User}? dbUser = null;

//        if (headers.CookieHash != null)
//        {{
//            dbToken = await db.Tokens
//                .OrderByDescending(a => a.Date)
//                .FirstOrDefaultAsync(a =>
//                    a.TokenHash == headers.CookieHash &&
//                    a.Date > longago,
//                    ct);
//        }}

//        if (dbToken != null &&
//            dbToken.Date > longago)
//        {{
//            dbUser = await db.Users
//                .Include(""CurrentCompany"")
//                .Include(""CurrentCompany.CompanyUsers"")
//                .Include(""CompanyUsers"")
//                .Include(""CompanyUsers.Company"")
//                .FirstOrDefaultAsync(a =>
//                    a.Id == dbToken.UserId,
//                    ct);
//            if (dbUser != null)
//            {{
//                if (dbToken.Date < shortago)
//                {{
//                    var cookieHash = headers.CreateNewCookie();
//                    dbToken = new Token(dbUser, cookieHash);
//                    await db.Tokens.AddAsync(dbToken, ct);
//                    await db.SaveChangesAsync(ct);
//                }}
//            }}
//        }}

//        var userId = dbUser?.Id;
//        var authenticationTokenId = dbToken?.Id;
//        var user = dbUser != null
//            ? await mapping.ToDtoAsync(dbUser, new StateUser(), ct)
//            : null;

//        var dbIp = await db.Ips
//            .FirstOrDefaultAsync(a =>
//                a.Address == headers.IpAdress.ToString(),
//                ct);
//        if (dbIp == null)
//        {{
//            dbIp = new {Ip}(headers.IpAdress.ToString());
//            await db.Ips.AddAsync(dbIp, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        _ = DoTheRest(headers, userId, authenticationTokenId, user, dbUser, dbToken, dbIp, ct);

//        return new {ServerAuthenticationState}(
//            user,
//            dbToken,
//            dbUser,
//            dbIp);
//    }}

//    public async Task<RequestIds> DoTheRest(
//        {AuthenticationHeaders.FullName} headers,
//        Guid? userId, long? authenticationTokenId,
//        {StateUser}? user, {User}? dbUser, {Token}? dbToken, {Ip} dbIp,
//        CancellationToken ct)
//    {{
//        if (config.UseMemoryDatabase)
//        {{
//            return await DoTheRest_EFCore(headers, userId, authenticationTokenId, user, dbUser, dbToken, dbIp, ct);
//        }}
//        return await DoTheRest_StoredProcedure(dbUser?.Id, dbToken?.Id, dbIp.Id, headers.SessionId, headers.Path);
//    }}

//    public async Task<{RequestIds}> DoTheRest_StoredProcedure(
//        Guid? userId, long? tokenId, long ipId, string sessionCode, string routePath)
//    {{
//        var sql = @""
//        DECLARE @SessionCode NVARCHAR(256) = @pSessionCode;
//        DECLARE @RoutePath NVARCHAR(256) = @pRoutePath;

//        DECLARE @UserId UNIQUEIDENTIFIER = @pUserId;
//        DECLARE @TokenId BIGINT = @pTokenId;
//        DECLARE @IpId BIGINT = @pIpId;
//        DECLARE @Now DATETIMEOFFSET = @pNow;

//        DECLARE @SessionId BIGINT;
//        DECLARE @RouteId BIGINT;
//        DECLARE @UserIpId BIGINT;
//        DECLARE @UserIpSessionId BIGINT;
//        DECLARE @UserIpSessionTokenId BIGINT;
//        DECLARE @UserIpSessionTokenRouteId BIGINT;
//        DECLARE @UserIpSessionTokenRouteRequestId BIGINT;

//        DECLARE @Counter INT

//        SET @Counter = 0;

//        -- =========================
//        -- Session
//        -- =========================
//        SELECT @SessionId = Id
//        FROM Sessions
//        WHERE SessionId = @SessionCode;

//        IF (@SessionId IS NULL)
//        BEGIN
//            INSERT INTO Sessions (SessionId)
//            VALUES (@SessionCode);

//            SET @SessionId = SCOPE_IDENTITY();
//        END       

//        -- =========================
//        -- Route
//        -- =========================
//        SELECT @RouteId = Id
//        FROM Routes
//        WHERE RouteName = @RoutePath;

//        IF (@RouteId IS NULL)
//        BEGIN
//            INSERT INTO Routes (RouteName)
//            VALUES (@RoutePath);

//            SET @RouteId = SCOPE_IDENTITY();
//        END

//        -- =========================
//        -- UserIp
//        -- =========================
//        SELECT @UserIpId = Id
//        FROM UserIps
//        WHERE UserId = @UserId
//          AND IpId = @IpId;

//        IF (@UserIpId IS NULL)
//        BEGIN
//            INSERT INTO UserIps (UserId, IpId)
//            VALUES (@UserId, @IpId);

//            SET @UserIpId = SCOPE_IDENTITY();
//        END

//        -- =========================
//        -- UserIpSession
//        -- =========================
//        SELECT @UserIpSessionId = Id
//        FROM UserIpSessions
//        WHERE UserIpId = @UserIpId
//          AND SessionId = @SessionId;

//        IF (@UserIpSessionId IS NULL)
//        BEGIN
//            INSERT INTO UserIpSessions (UserIpId, SessionId)
//            VALUES (@UserIpId, @SessionId);

//            SET @UserIpSessionId = SCOPE_IDENTITY();
//        END

//        -- =========================
//        -- UserIpSessionToken
//        -- =========================
//        SELECT @UserIpSessionTokenId = Id
//        FROM UserIpSessionTokens
//        WHERE UserIpSessionId = @UserIpSessionId
//          AND TokenId = @TokenId;

//        IF (@UserIpSessionTokenId IS NULL)
//        BEGIN
//            INSERT INTO UserIpSessionTokens
//                (UserIpSessionId, TokenId)
//            VALUES
//                (@UserIpSessionId, @TokenId);

//            SET @UserIpSessionTokenId = SCOPE_IDENTITY();
//        END

//        -- =========================
//        -- UserIpSessionTokenRoute
//        -- =========================
//        SELECT @UserIpSessionTokenRouteId = Id
//        FROM UserIpSessionTokenRoutes
//        WHERE UserIpSessionTokenId = @UserIpSessionTokenId
//          AND RouteId = @RouteId;

//        IF (@UserIpSessionTokenRouteId IS NULL)
//        BEGIN
//            INSERT INTO UserIpSessionTokenRoutes
//                (UserIpSessionTokenId, RouteId)
//            VALUES
//                (@UserIpSessionTokenId, @RouteId);

//            SET @UserIpSessionTokenRouteId = SCOPE_IDENTITY();
//        END

//        -- =========================
//        -- UserIpSessionTokenRouteRequest (ALTIJD NIEUW)
//        -- =========================
//        SELECT @UserIpSessionTokenRouteRequestId = Id, @Counter = [Count]
//        FROM UserIpSessionTokenRouteRequests
//        WHERE UserIpSessionTokenRouteId = @UserIpSessionTokenRouteId
//        AND [Year] = YEAR(@Now)
//        AND [Month] = MONTH(@Now)
//        AND [Day] = DAY(@Now)
//        AND [Hour] = DATEPART(HOUR, @Now);

//        IF (@UserIpSessionTokenRouteRequestId IS NULL)
//        BEGIN
//            INSERT INTO UserIpSessionTokenRouteRequests
//                (UserIpSessionTokenRouteId, [Year], [Month], [Day], [Hour], [Count])
//            VALUES
//                (@UserIpSessionTokenRouteId, YEAR(@Now), MONTH(@Now), DAY(@Now), DATEPART(HOUR, @Now), 1);

//            SET @UserIpSessionTokenRouteRequestId = SCOPE_IDENTITY();
//        END
//        ELSE
//        BEGIN
//            UPDATE UserIpSessionTokenRouteRequests
//            SET 
//                [Count] = @Counter + 1
//            WHERE Id = @UserIpSessionTokenRouteRequestId;        
//        END        

//        SELECT
//            @SessionId AS SessionId,
//            @RouteId AS RouteId,
//            @UserIpId AS UserIpId,
//            @UserIpSessionId AS UserIpSessionId,
//            @UserIpSessionTokenId AS UserIpSessionTokenId,
//            @UserIpSessionTokenRouteId AS UserIpSessionTokenRouteId,
//            @UserIpSessionTokenRouteRequestId AS UserIpSessionTokenRouteRequestId,
//            @Counter + 1 AS Counter;

//        "";

//        var parameters = new SqlParameter[]
//        {{
//            new(""@pSessionCode"", SqlDbType.NVarChar) {{ Value = sessionCode }},
//            new(""@pRoutePath"", SqlDbType.NVarChar) {{ Value = routePath }},

//            new(""@pUserId"", SqlDbType.UniqueIdentifier) {{ Value = (object?)userId ?? DBNull.Value }},
//            new(""@pTokenId"", SqlDbType.BigInt) {{ Value = (object?)tokenId ?? DBNull.Value }},
//            new(""@pIpId"", SqlDbType.BigInt) {{ Value = ipId }},
//            new(""@pNow"", SqlDbType.DateTimeOffset) {{ Value = dateTime.GetUtcNow() }}
//        }};

//        var result = db.Database
//            .SqlQueryRaw<RequestIds>(sql, parameters)
//            .AsEnumerable()
//            .Single();

//        return result;
//    }}

//    public async Task<RequestIds> DoTheRest_EFCore(
//        {AuthenticationHeaders.FullName} headers,
//        Guid? userId, long? authenticationTokenId,
//        {StateUser}? user, {User}? dbUser, {Token}? dbToken, {Ip} dbIp,
//        CancellationToken ct)
//    {{
//        var dbSession = await db.Sessions
//            .FirstOrDefaultAsync(a =>
//                a.SessionId == headers.SessionId,
//                ct);
//        if (dbSession == null)
//        {{
//            dbSession = new {Session}(headers.SessionId);
//            await db.Sessions.AddAsync(dbSession, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        var dbRoute = await db.Routes
//            .FirstOrDefaultAsync(a =>
//                a.RouteName == headers.EncodedPath,
//                ct);
//        if (dbRoute == null)
//        {{
//            dbRoute = new {Route}(headers.EncodedPath);
//            await db.Routes.AddAsync(dbRoute, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        var dbUserIp = await db.UserIps
//            .FirstOrDefaultAsync(a =>
//                a.UserId == userId &&
//                a.IpId == dbIp.Id,
//                ct);
//        if (dbUserIp == null)
//        {{
//            dbUserIp = new {UserIp}(dbUser, dbIp);
//            await db.UserIps.AddAsync(dbUserIp, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        var dbUserIpSession = await db.UserIpSessions
//            .FirstOrDefaultAsync(a =>
//                a.UserIpId == dbUserIp.Id &&
//                a.SessionId == dbSession.Id, 
//                ct);
//        if (dbUserIpSession == null)
//        {{
//            dbUserIpSession = new {UserIpSession}(dbUserIp, dbSession);
//            await db.UserIpSessions.AddAsync(dbUserIpSession, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        var dbUserIpSessionToken = await db.UserIpSessionTokens
//            .FirstOrDefaultAsync(a =>
//                a.TokenId == authenticationTokenId &&
//                a.UserIpSessionId == dbUserIpSession.Id,
//                ct);
//        if (dbUserIpSessionToken == null)
//        {{
//            dbUserIpSessionToken = new {UserIpSessionToken}(dbUserIpSession, dbToken);
//            await db.UserIpSessionTokens.AddAsync(dbUserIpSessionToken, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        var dbUserIpSessionTokenRoute = await db.UserIpSessionTokenRoutes
//            .FirstOrDefaultAsync(a =>
//                a.RouteId == dbRoute.Id &&
//                a.UserIpSessionTokenId == dbUserIpSessionToken.Id,
//                ct);
//        if (dbUserIpSessionTokenRoute == null)
//        {{
//            dbUserIpSessionTokenRoute = new {UserIpSessionTokenRoute}(dbUserIpSessionToken, dbRoute);
//            await db.UserIpSessionTokenRoutes.AddAsync(dbUserIpSessionTokenRoute, ct);
//            await db.SaveChangesAsync(ct);
//        }}

//        var dbUserIpSessionTokenRouteRequest = new {UserIpSessionTokenRouteRequest}(dbUserIpSessionTokenRoute, dateTime.GetUtcNow());
//        await db.UserIpSessionTokenRouteRequests.AddAsync(dbUserIpSessionTokenRouteRequest, ct);
//        await db.SaveChangesAsync(ct);

//        return new {RequestIds}()
//        {{
//            Counter = 1,
//            RouteId = dbRoute.Id,
//            SessionId = dbSession.Id,
//            UserIpId = dbUserIp.Id,
//            UserIpSessionId = dbUserIpSession.Id,
//            UserIpSessionTokenId = dbUserIpSessionToken.Id,
//            UserIpSessionTokenRouteId = dbUserIpSessionTokenRoute.Id,
//            UserIpSessionTokenRouteRequestId = dbUserIpSessionTokenRouteRequest.Id,
//        }};
//    }}
//}}";
//        Save(false);
//    }
//}
