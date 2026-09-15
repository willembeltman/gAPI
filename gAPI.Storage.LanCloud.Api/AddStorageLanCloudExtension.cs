using gAPI.Generated;
using gAPI.Storage.LanCloud.Api.Collections;
using gAPI.Storage.LanCloud.Api.Ftp;
using gAPI.Storage.LanCloud.Api.Interfaces;
using gAPI.Storage.LanCloud.Api.Services;
using gAPI.Storage.LanCloud.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Storage.LanCloud.Api;

public static class AddStorageLanCloudExtension
{
    public static WebApplicationBuilder AddStorageLanCloud(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        }

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddAutoWssServer();
        builder.Services.AddAutoAuthServer();
        builder.Services.AddControllers(); // For the WebDav controller
        builder.Services.AddHostedService<FtpServer>();
        builder.Services.AddScoped<IFileSystemDirect, FileSystem>();
        builder.Services.AddSingleton<RespondedEntryCollection>();

        var localShareDirectory = Path.Combine(Environment.CurrentDirectory, "LocalData");
        var localShare = new LocalShare(localShareDirectory);
        var apiConfig = new LanCloudApiConfig(localShare);
        builder.Services.AddSingleton(apiConfig);

        return builder;
    }
    public static WebApplication MapStorageLanCloud(this WebApplication app)
    {
        app.MapAutoWssServer();
        app.MapAutoAuthServer();
        app.MapControllers(); // for the WebDav controller
        app.UseHttpsRedirection();

        return app;
    }
}
