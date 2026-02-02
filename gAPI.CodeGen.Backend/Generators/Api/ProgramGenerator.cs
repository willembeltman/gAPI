using gAPI.CodeGen.Backend.Generators.Core.Authentication;
using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Api;

public class ProgramGenerator : BaseGenerator
{
    public ProgramGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Api_Directory;
        Namespace = null;

        Context = context;

        Name = "Program";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public AddCommenServicesExtentionGenerator AddCommenServicesExtention => Context.AddCommenServicesExtention;
    public AddCrudExtentionsGenerator AddCrudExtention => Context.AddCrudExtention;
    public AddDatabaseExtentionGenerator AddDatabaseExtention => Context.AddDatabaseExtention;
    public AddRemainingAuthenticationServicesExtentionGenerator AddRemainingAuthenticationServicesExtention => Context.AddRemainingAuthenticationServicesExtention;
    public SharedReference CreateServerConfigExtention => Context.SharedReferences.CreateServerConfigExtention;
    public SharedReference AddAutoApiExtention => Context.SharedReferences.AddAutoApiExtention;
    public SharedReference AddAutoSseExtention => Context.SharedReferences.AddAutoSseExtention;
    public SharedReference AddStorageExtention => Context.SharedReferences.AddStorageExtention;
    public IServerAuthenticationServiceGenerator IServerAuthenticationService => Context.IServerAuthenticationService;
    public ServerAuthenticationServiceGenerator ServerAuthenticationService => Context.ServerAuthenticationService;
    public ServerAuthenticationHandlerGenerator ServerAuthenticationHandler => Context.ServerAuthenticationHandler;
    public ServerAuthenticationMiddlewareGenerator ServerAuthenticationMiddleware => Context.ServerAuthenticationMiddleware;

    public void GenerateCode()
    {
        Reg("Microsoft.EntityFrameworkCore");
        Reg("Scalar.AspNetCore");
        Reg(CreateServerConfigExtention);
        Reg(IServerAuthenticationService);
        Reg(AddCommenServicesExtention);
        Reg(AddCrudExtention);
        Reg(AddDatabaseExtention);
        Reg(AddRemainingAuthenticationServicesExtention);
        Reg(AddAutoApiExtention);
        Reg(AddAutoSseExtention);
        Reg(AddStorageExtention);
        Reg(ServerAuthenticationService);
        Reg(ServerAuthenticationMiddleware);

        Code = $@"{GetNamespacesCode()}
var builder = WebApplication.CreateBuilder(args);

// Configuration setup
if (builder.Environment.IsDevelopment())
{{
    builder.Configuration.AddJsonFile(""appsettings.Development.json"", optional: true, reloadOnChange: true);
}}
builder.Configuration.AddEnvironmentVariables();

// Registrate services
var serverConfig = builder.Configuration.CreateServerConfig();
builder.Services.AddCommenServices(serverConfig);
builder.Services.AddAutoApi<{IServerAuthenticationService}, {ServerAuthenticationService}, {ServerAuthenticationHandler}>(serverConfig.FrontendUrl);
builder.Services.AddAutoSse(serverConfig.FabricConnectionString);
builder.Services.AddDatabase(serverConfig.DefaultConnectionString, serverConfig.UseMemoryDatabase);
builder.Services.AddStorage(serverConfig.StorageConnectionString);
builder.Services.AddRemainingAuthenticationServices();
builder.Services.AddCrudUseCases();
builder.Services.AddCrudMappings();

if (builder.Environment.IsDevelopment())
{{
    builder.Services.AddOpenApi();
}}

// Finally build the app
var app = builder.Build();
app.MapAutoApi<{ServerAuthenticationMiddleware}>();
app.MapAutoSse();
app.MapDatabase();

if (app.Environment.IsDevelopment())
{{
    app.MapOpenApi();
    app.MapScalarApiReference();
}}

// App started logo
Console.WriteLine(""##################################"");
Console.WriteLine(""##                              ##"");
Console.WriteLine(""##       ##       ######   ##   ##"");
Console.WriteLine(""##      ####      ##   ##  ##   ##"");
Console.WriteLine(""##     ##  ##     ##   ##  ##   ##"");
Console.WriteLine(""##    ########    ######   ##   ##"");
Console.WriteLine(""##   ##      ##   ##       ##   ##"");
Console.WriteLine(""##  ##        ##  ##       ##   ##"");
Console.WriteLine(""##                              ##"");
Console.WriteLine(""##          JUST STARTED        ##"");
Console.WriteLine(""##                              ##"");
Console.WriteLine(""##################################"");
Console.WriteLine(""## FRONTEND URL = "" + serverConfig.FrontendUrl);
Console.WriteLine(""## USE MEMORY DATABASE = "" + serverConfig.UseMemoryDatabase);
if (builder.Environment.IsDevelopment())
{{
    Console.WriteLine(""## DEFAULT CONNECTIONSTRING = "" + serverConfig.DefaultConnectionString);
    Console.WriteLine(""## STORAGE CONNECTIONSTRING = "" + serverConfig.StorageConnectionString);
    Console.WriteLine(""## FABRIC CONNECTIONSTRING = "" + serverConfig.FabricConnectionString);
}}

app.Run();";
        Save(false);
    }
}
