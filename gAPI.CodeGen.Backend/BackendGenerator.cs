using gAPI.CodeGen.Backend.Generators.Api;
using gAPI.CodeGen.Backend.Generators.Core.CrudMappings;

//using gAPI.CodeGen.Backend.Generators.Core.Services;
//using gAPI.CodeGen.Backend.Generators.Data.Authentication;
using gAPI.CodeGen.Backend.Generators.Shared.Public.Dtos;
//using gAPI.CodeGen.Backend.Generators.Shared.Interfaces;
//using gAPI.CodeGen.Backend.Generators.Shared.RequestDtos;
//using gAPI.CodeGen.Backend.Generators.Shared.ResponseDtos;
using gAPI.CodeGen.Backend.Generators.Shared.StateDtos;
using gAPI.CodeGen.Backend.Models;
using gAPI.CodeGen.Backend.Models.Config;

namespace gAPI.CodeGen.Backend;

public class BackendGenerator
{
    public BackendGenerator(BackendConfig config)
    {
        Config = config;

        DbContext = new DbContext(config.DbContextType);
        SharedReferences = new SharedReferences(config);

        //// Data.Authentication
        //Ip = new IpGenerator(this);
        //Route = new RouteGenerator(this);
        //Session = new SessionGenerator(this);
        //Token = new TokenGenerator(this);
        //UserIp = new UserIpGenerator(this);
        //UserIpSession = new UserIpSessionGenerator(this);
        //UserIpSessionToken = new UserIpSessionTokenGenerator(this);
        //UserIpSessionTokenRoute = new UserIpSessionTokenRouteGenerator(this);
        //UserIpSessionTokenRouteRequest = new UserIpSessionTokenRouteRequestGenerator(this);

        // Bsd.Shared.Public.Dtos 
        Dtos = [.. DbContext.DbSets
            .Where(a => !a.Entity.IsHidden)
            .Select(dbSet => new DtoGenerator(this, dbSet))];

        //Shared.StateDtos
        StateUser = new StateDtoGenerator(this, DbContext.StateUser);
        StateObjects = [.. DbContext.StateObjects
            .GroupBy(a => a.ClassName)
            .Select(a => new StateDtoGenerator(this, a.First()))];
        State = new StateGenerator(this, StateUser);

        //// Core.Authentication
        //RequestIds = new RequestIdsGenerator(this);
        //IServerAuthenticationService = new IServerAuthenticationServiceGenerator(this);
        //IServerAuthenticationStateFactory = new IServerAuthenticationStateFactoryGenerator(this);
        //IServerAuthenticationSecurity = new IServerAuthenticationSecurityGenerator(this);
        //ServerAuthenticationService = new ServerAuthenticationServiceGenerator(this);
        //ServerAuthenticationState = new ServerAuthenticationStateGenerator(this);
        //ServerAuthenticationHandler = new ServerAuthenticationHandlerGenerator(this);
        StateMapping = new StateMappingGenerator(this);

        // Api
        //AddCommenServicesExtension = new AddCommenServicesExtensionGenerator(this);
        AddCrudExtension = new AddCrudExtensionsGenerator(this);
        AddDatabaseExtension = new AddDatabaseExtensionGenerator(this);
        //AddRemainingAuthenticationServicesExtension = new AddRemainingAuthenticationServicesExtensionGenerator(this);
        //Program = new ProgramGenerator(this);
        //ServerAuthenticationMiddleware = new ServerAuthenticationMiddlewareGenerator(this);

        //LoginResponse = new LoginResponseGenerator(this);
        //RegisterRequest = new RegisterRequestGenerator(this);
        //ForgotPasswordRequest = new ForgotPasswordRequestGenerator(this);
        //ChangePasswordRequest = new ChangePasswordRequestGenerator(this);

        //LoginRequest = new LoginRequestGenerator(this);
        //RegisterResponse = new RegisterResponseGenerator(this);
        //ForgotPasswordResponse = new ForgotPasswordResponseGenerator(this);
        //ChangePasswordResponse = new ChangePasswordResponseGenerator(this);

        //IAuthenticationService = new IAuthenticationServiceGenerator(this);
        //AuthenticationService = new AuthenticationServiceGenerator(this);
        //IManageService = new IManageServiceGenerator(this);
        //ManageService = new ManageServiceGenerator(this);

        //ServerAuthenticationStateFactory = new ServerAuthenticationStateFactoryGenerator(this);
        //ServerAuthenticationSecurity = new ServerAuthenticationSecurityGenerator(this);
    }

    public BackendConfig Config { get; }
    public DbContext DbContext { get; }
    public SharedReferences SharedReferences { get; }

    public StateGenerator State { get; }
    //public IServerAuthenticationServiceGenerator IServerAuthenticationService { get; }
    //public ServerAuthenticationStateGenerator ServerAuthenticationState { get; }
    public DtoGenerator[] Dtos { get; }

    //public UserIpSessionGenerator UserIpSession { get; }
    //public TokenGenerator Token { get; }
    //public IpGenerator Ip { get; }
    //public RouteGenerator Route { get; }
    //public SessionGenerator Session { get; }
    //public UserIpGenerator UserIp { get; }
    //public UserIpSessionTokenGenerator UserIpSessionToken { get; }
    //public UserIpSessionTokenRouteGenerator UserIpSessionTokenRoute { get; }
    //public UserIpSessionTokenRouteRequestGenerator UserIpSessionTokenRouteRequest { get; }
    ///public AddCommenServicesExtensionGenerator AddCommenServicesExtension { get; }
    public AddCrudExtensionsGenerator AddCrudExtension { get; }
    public AddDatabaseExtensionGenerator AddDatabaseExtension { get; }
    //public AddRemainingAuthenticationServicesExtensionGenerator AddRemainingAuthenticationServicesExtension { get; }
    //public ProgramGenerator Program { get; }
    //public ServerAuthenticationServiceGenerator ServerAuthenticationService { get; }
    public StateMappingGenerator StateMapping { get; }
    //public IServerAuthenticationStateFactoryGenerator IServerAuthenticationStateFactory { get; }
    //public ServerAuthenticationHandlerGenerator ServerAuthenticationHandler { get; }
    //public IServerAuthenticationSecurityGenerator IServerAuthenticationSecurity { get; }
    //public RequestIdsGenerator RequestIds { get; }
    //public ServerAuthenticationMiddlewareGenerator ServerAuthenticationMiddleware { get; set; }
    public StateDtoGenerator StateUser { get; }
    public StateDtoGenerator[] StateObjects { get; }
    //public LoginResponseGenerator LoginResponse { get; }
    //public LoginRequestGenerator LoginRequest { get; }
    //public RegisterRequestGenerator RegisterRequest { get; }
    //public ForgotPasswordRequestGenerator ForgotPasswordRequest { get; }
    //public RegisterResponseGenerator RegisterResponse { get; }
    //public ForgotPasswordResponseGenerator ForgotPasswordResponse { get; }
    //public IAuthenticationServiceGenerator IAuthenticationService { get; }
    //public AuthenticationServiceGenerator AuthenticationService { get; }
    //public ManageServiceGenerator ManageService { get; }
    //public ServerAuthenticationStateFactoryGenerator ServerAuthenticationStateFactory { get; }
    //public ServerAuthenticationSecurityGenerator ServerAuthenticationSecurity { get; }
    //public IManageServiceGenerator IManageService { get; }
    //public ChangePasswordRequestGenerator ChangePasswordRequest { get; }
    //public ChangePasswordResponseGenerator ChangePasswordResponse { get; }
    public StateDtoGenerator[] AllStateObjects => [StateUser, .. StateObjects];

    public void Run()
    {
        State.GenerateCode();
        //IServerAuthenticationService.GenerateCode();
        //ServerAuthenticationState.GenerateCode();
        foreach (var dto in Dtos) dto.GenerateCode();
        foreach (var stateObject in StateObjects) stateObject.GenerateCode();

        //UserIpSession.GenerateCode();
        //Token.GenerateCode();
        //Ip.GenerateCode();
        //Route.GenerateCode();
        //Session.GenerateCode();
        //UserIp.GenerateCode();
        //UserIpSessionToken.GenerateCode();
        //UserIpSessionTokenRoute.GenerateCode();
        //UserIpSessionTokenRouteRequest.GenerateCode();
        //AddCommenServicesExtension.GenerateCode();
        AddCrudExtension.GenerateCode();
        AddDatabaseExtension.GenerateCode();
        //AddRemainingAuthenticationServicesExtension.GenerateCode();
        //Program.GenerateCode();
        //ServerAuthenticationService.GenerateCode();
        StateMapping.GenerateCode();
        //IServerAuthenticationStateFactory.GenerateCode();
        //ServerAuthenticationHandler.GenerateCode();
        //IServerAuthenticationSecurity.GenerateCode();
        //RequestIds.GenerateCode();
        //ServerAuthenticationMiddleware.GenerateCode();
        StateUser.GenerateCode();
        //LoginResponse.GenerateCode();
        //LoginRequest.GenerateCode();
        //RegisterRequest.GenerateCode();
        //ForgotPasswordRequest.GenerateCode();
        //RegisterResponse.GenerateCode();
        //ForgotPasswordResponse.GenerateCode();
        //IAuthenticationService.GenerateCode();
        //AuthenticationService.GenerateCode();
        //ManageService.GenerateCode();
        //ServerAuthenticationStateFactory.GenerateCode();
        //ServerAuthenticationSecurity.GenerateCode();
        //IManageService.GenerateCode();
        //ChangePasswordRequest.GenerateCode();
        //ChangePasswordResponse.GenerateCode();

    }
}
