using gAPI.AutoApiClient.Generators;
using gAPI.AutoApiClient.Models;
using gAPI.AutoApiClient.Models.Configs;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiClient;

public class Generator
{
    public Generator(
        ClientConfig config,
        SharedReferences sharedReferences,
        ServiceContext serviceContext, 
        Microsoft.CodeAnalysis.SourceProductionContext spc)
    {
        Config = config;
        SharedReferences = sharedReferences;
        ServiceContext = serviceContext;
        this.spc = spc;

        FormFile = new FormFileGenerator(Config);
        IsFormFileExtention = new IsFormFileExtentionGenerator(Config);
        //ItemDataSource = new ItemDataSourceGenerator(Config);
        //ListDataSource = new ListDataSourceGenerator(Config);
        Clients = ServiceContext.Interfaces
            .Select(service => new ApiClientGenerator(service, this, Config))
            .ToArray();
        AddAutoClientServices = new AddAutoClientServicesGenerator(Clients, Config);
    }

    public ServiceContext ServiceContext { get; }

    private Microsoft.CodeAnalysis.SourceProductionContext spc;

    public ClientConfig Config { get; }
    public ApiClientGenerator[] Clients { get; }
    public AddAutoClientServicesGenerator AddAutoClientServices { get; }
    public FormFileGenerator FormFile { get; }
    public IsFormFileExtentionGenerator IsFormFileExtention { get; }
    public SharedReferences SharedReferences { get;  }

    internal void Generate()
    {
        FormFile.GenerateCode();
        var formFileFullName = Path.Combine(Config.Helpers_Destination.Directory, FormFile.FileName);
        spc.AddSource(formFileFullName, SourceText.From(FormFile.Code, Encoding.UTF8));

        IsFormFileExtention.GenerateCode();
        var toFormFileExtentionFullName = Path.Combine(Config.Helpers_Destination.Directory, IsFormFileExtention.FileName);
        spc.AddSource(toFormFileExtentionFullName, SourceText.From(IsFormFileExtention.Code, Encoding.UTF8));

        //ItemDataSource.GenerateCode();
        //var itemDataSourceFullName = Path.Combine(Config.Helpers_Destination.Directory, ItemDataSource.FileName);
        //spc.AddSource(itemDataSourceFullName, SourceText.From(ItemDataSource.Code, Encoding.UTF8));

        //ListDataSource.GenerateCode();
        //var listDataSourceFullName = Path.Combine(Config.Helpers_Destination.Directory, ListDataSource.FileName);
        //spc.AddSource(listDataSourceFullName, SourceText.From(ListDataSource.Code, Encoding.UTF8));

        foreach (var client in Clients)
        {
            client.GenerateCode();
            var clientFullName = Path.Combine(Config.Clients_Destination.Directory, client.FileName);
            spc.AddSource(clientFullName, SourceText.From(client.Code, Encoding.UTF8));
        }

        AddAutoClientServices.GenerateCode();
        var addAutoClientServicesFullName = Path.Combine(Config.Clients_Destination.Directory, AddAutoClientServices.FileName);
        spc.AddSource(addAutoClientServicesFullName, SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));
    }
}