# gAPI.Storage

A .NET 9.0 client library for connecting to a gAPI.Storage.Server instance.
This package uses the shared gAPI.Storage library to provide seamless communication and file transfer between your application and a remote storage server.

## How it works

- Exposes the IStorageService interface and StorageService implementation for interacting with remote storage.
- Handles authentication and file transfer logic internally.
- Automatically generates a gapistoragesettings.json file when installed.

## Quick registration

    builder.Services.AddScoped<IStorageService, StorageService>();

## Configuration

The generated gapistoragesettings.json should be linked to your application configuration so the storage service can read connection details:

    {
      "ConnectionStrings": {
        "StorageConnection": "Provider=Mock;BaseUrl=https://some.url/;LatencyMs=10"
      }
    }

ConnectionString can be:
- `Provider=Mock;BaseUrl=<...>;LatencyMs=<...>` for local development with mock data.
- `Provider=Azure;ContainerName=<...>;UseDevelopmentStorage=true` for azure local development with azurite.
- `Provider=Azure;ContainerName=<...>;DefaultEndpointsProtocol=https;AccountName=<...>;AccountKey=<...>;EndpointSuffix=core.windows.net` for Azure Blob Storage.
- `Provider=StorageServer;Server=<...>;Username=<...>;Password=<...>` for the gAPI storage server.

Register the configuration in your program setup:

    builder.Configuration.AddJsonFile(
        "gapistoragesettings.json", 
        optional: false, 
        reloadOnChange: true
    );

## Features

- Simple API for uploading, downloading, and managing files stored on a remote gAPI storage server.
- Built on the shared gAPI.Storage library for consistent data contracts.
- Works seamlessly with entities implementing IStorageFile.
