# gAPI.Storage.Server

## A lightweight storage server package for ASP.NET Core 9.0.
By installing this NuGet package into a blank ASP.NET project, you instantly transform it into a fully functional storage server.

## How it works

- Stores both file data and the metadata database directly within the website’s hosting environment.
- Designed for one-time deployment — after publishing, the storage server should not require further updates.
- Uses gAPI.EntityFrameworkDisk to store the database directly on disk inside the web project.
- Uses gAPI.Storage as a shared library for core storage operations.

## Features

- Minimal setup — simply install the package to enable storage server functionality.
- Automatic creation of a gapistoragesettings.json configuration file in your project root.
- Easy authentication setup with credentials stored in the configuration file.
- Includes Swagger support for API exploration.

## Sample configuration (gapistoragesettings.json):

    {
      "LocalStorageServerConfig": {
        "SuperSecretKey": "SUPERSECRETKEYOF32CHARACTERSLONG",
        "Credentials": [
          {
            "UserName": "USERNAME",
            "Password": "PASSWORD"
          }
        ]
      }
    }

## Quick start — Minimal Program.cs example:

    using gAPI.Storage.Server;

    var builder = WebApplication.CreateBuilder(args);

    // This fully registrates the service
    builder.RegistrateServices();

    // Add swagger if you like
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Build the app
    var app = builder.Build();

    // We are using Jwt token based communication so this is required
    app.UseAuthentication();
    app.UseAuthorization();

    // Add swagger if you like
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseHttpsRedirection();
    app.MapControllers();
    app.Run();