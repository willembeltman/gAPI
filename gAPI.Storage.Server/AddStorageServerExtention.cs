using gAPI.Storage.Server.Config;
using gAPI.Storage.Server.Data;
using gAPI.Storage.Server.Data.Entities;
using gAPI.Storage.Server.Services;
using gAPI.Storage.StorageServer.Dtos.Requests;
using gAPI.Storage.StorageServer.Dtos.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace gAPI.Storage.Server;

public static class AddStorageServerExtention
{
    public static WebApplicationBuilder AddStorageServer(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        }
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.Configure<LocalStorageServerConfig>(
            builder.Configuration.GetSection("LocalStorageServerConfig")
        );

        var appConfig = builder.Configuration.GetSection("LocalStorageServerConfig").Get<LocalStorageServerConfig>()!;

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                //options.RequireHttpsMetadata = false; // for local dev
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(appConfig.SuperSecretKeyArray)
                };
            });

        builder.Services.AddScoped<LocalStorageService>();
        builder.Services.AddSingleton<ApplicationDbContext>();
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        Console.WriteLine("#########################################################");
        Console.WriteLine("##                                                     ##");
        Console.WriteLine("##  ###  ######  ####   ####      ###     ####   ##### ##");
        Console.WriteLine("## ## ##   ##   ##  ##  ##  ##   ## ##   ##  ##  ##    ##");
        Console.WriteLine("## ##      ##   ##  ##  ##  ##  ##   ##  ##      ##    ##");
        Console.WriteLine("##  ###    ##   ##  ##  #####   #######  ## #### ####  ##");
        Console.WriteLine("##    ##   ##   ##  ##  ## ##   ##   ##  ##  ##  ##    ##");
        Console.WriteLine("## ## ##   ##   ##  ##  ##  ##  ##   ##  ##  ##  ##    ##");
        Console.WriteLine("##  ###    ##    ####   ##  ##  ##   ##   ####   ##### ##");
        Console.WriteLine("##                                                     ##");
        Console.WriteLine("#########################################################");
        Console.WriteLine($"## gAPI.Storage.Server.WebApplicationBuilderExtention UserName = {appConfig.Credentials.UserName}");

        return builder;
    }


    public static WebApplication MapStorageServer(this WebApplication app)
    {
        app.MapControllers();

        //#region Auth controller

        //app.MapPost("/Auth/Login", (IOptions<LocalStorageServerConfig> config, [FromBody] LoginRequest request) =>
        //{
        //    //Console.WriteLine($"gAPI.Storage.Server.Controllers.AuthController / Login UserName={config.Value.Credentials.UserName} token={config.Value.SuperSecretKeyArray}");
        //    var cred = config.Value.Credentials.UserName == request.Username && config.Value.Credentials.Password == request.Password;

        //    if (!cred)
        //        return Results.Unauthorized();
        //    var key = new SymmetricSecurityKey(config.Value.SuperSecretKeyArray);
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Expires = DateTime.UtcNow.AddHours(1),
        //        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        //    };

        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    var jwt = tokenHandler.WriteToken(token);

        //    return Results.Ok(new { token = jwt });
        //});

        //#endregion
        //#region Storage controller

        //app.MapPost("/Storage/GetStorageFileInfo", (LocalStorageService storageService, [FromBody] GetStorageFileInfoRequest model) =>
        //{
        //    return storageService.GetStorageFileUrl(model);
        //})
        //.RequireAuthorization(); 

        //app.MapPost("/Storage/SaveStorageFile", (LocalStorageService storageService, [FromForm] SaveRequest model, IFormFile file) =>
        //{
        //    if (file == null || file.Length == 0)
        //        return new SaveResponse { Success = false, Message = "No file uploaded" };

        //    var stream = file.OpenReadStream();
        //    return storageService.SaveStorageFile(model, stream);
        //})
        //.RequireAuthorization();

        //app.MapPost("/Storage/DeleteStorageFile", (LocalStorageService storageService, [FromBody] DeleteRequest model) =>
        //{
        //    return storageService.DeleteStorageFile(model);
        //})
        //.RequireAuthorization();

        //#endregion
        //#region Content controller

        //// GET /Content/{*path}?token=...
        //app.MapGet("/Content/{*path}", (
        //    LocalStorageService storageService,
        //    ApplicationDbContext db,
        //    string path,
        //    string token
        //) =>
        //{
        //    if (string.IsNullOrWhiteSpace(token))
        //        return Results.Unauthorized();

        //    if (string.IsNullOrWhiteSpace(path))
        //        return Results.NotFound($"File '{path}' not found.");

        //    var split = path.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        //    if (split.Length < 2)
        //        return Results.NotFound($"File '{path}' not found.");

        //    var directoryName = split[0];
        //    var fileName = split[1];

        //    if (!storageService.TryGetFile(path, token, directoryName, fileName, out string fullName, out StorageFile file, out string denyReason))
        //        return Results.NotFound(denyReason);

        //    return Results.File(
        //        path: fullName,
        //        contentType: file.MimeType,
        //        fileDownloadName: file.FileName
        //    );
        //});

        //#endregion

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
