using gAPI.AutoApiClient.Generators;
using gAPI.AutoApiClient.Models;
using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiClient;

public class Generator
{
    public Generator(
        ServiceContext serviceContext,
        SharedReferences sharedReferences,
        CustomObjectMethod[] customMultipartFormDataContentSerializers)
    {
        SharedReferences = sharedReferences;
        ServiceContext = serviceContext;
        CustomMultipartFormDataContentSerializers = customMultipartFormDataContentSerializers;

        FormFile = new FormFileGenerator(this);
        IsFormFileExtension = new FormFileExtensionGenerator(this);
        Clients = ServiceContext.ApiInterfaces
            .Concat(ServiceContext.MinimalApiInterfaces)
            .Select(service => new ApiClientGenerator(this, service, customMultipartFormDataContentSerializers))
            .ToArray();
        AddAutoClientServices = new AutoApiClientExtensionGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContentSerializers { get; }
    public SharedReferences SharedReferences { get; }

    public ApiClientGenerator[] Clients { get; }
    public AutoApiClientExtensionGenerator AddAutoClientServices { get; }
    public FormFileGenerator FormFile { get; }
    public FormFileExtensionGenerator IsFormFileExtension { get; }

    public void Generate(Microsoft.CodeAnalysis.SourceProductionContext spc)
    {
        FormFile.GenerateCode();
        if (!string.IsNullOrEmpty(FormFile.Code))
        {
            var formFileFullName = Path.Combine(FormFile.Directory, FormFile.FileName);
            spc.AddSource(formFileFullName, SourceText.From(FormFile.Code, Encoding.UTF8));
        }

        IsFormFileExtension.GenerateCode();
        if (!string.IsNullOrEmpty(IsFormFileExtension.Code))
        {
            var toFormFileExtensionFullName = Path.Combine(IsFormFileExtension.Directory, IsFormFileExtension.FileName);
            spc.AddSource(toFormFileExtensionFullName, SourceText.From(IsFormFileExtension.Code, Encoding.UTF8));
        }

        //ItemDataSource.GenerateCode();
        //var itemDataSourceFullName = Path.Combine(Config.Helpers_Destination.Directory, ItemDataSource.FileName);
        //spc.AddSource(itemDataSourceFullName, SourceText.From(ItemDataSource.Code, Encoding.UTF8));

        //ListDataSource.GenerateCode();
        //var listDataSourceFullName = Path.Combine(Config.Helpers_Destination.Directory, ListDataSource.FileName);
        //spc.AddSource(listDataSourceFullName, SourceText.From(ListDataSource.Code, Encoding.UTF8));

        foreach (var client in Clients)
        {
            client.GenerateCode();
            var clientFullName = Path.Combine(client.Directory, client.FileName);
            spc.AddSource(clientFullName, SourceText.From(client.Code, Encoding.UTF8));
        }

        AddAutoClientServices.GenerateCode();
        var addAutoClientServicesFullName = Path.Combine(AddAutoClientServices.Directory, AddAutoClientServices.FileName);
        spc.AddSource(addAutoClientServicesFullName, SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));

        HashSet<string> added = [];

        foreach (var api in Clients)
        {
            var items = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(api.NeededSerializers.ToArray(), CustomMultipartFormDataContentSerializers.Select(a => a.Type));
            foreach (var item in items)
            {
                if (added.Add(item.Name))
                {
                    var serializerGenerator = new MultipartFormDataContentSerializerGenerator(item, CustomMultipartFormDataContentSerializers);
                    //serializerGenerator.Namespace = api.Namespace!;
                    var code = serializerGenerator.Generate();
                    spc.AddSource(
                        serializerGenerator.FileName,
                        SourceText.From(code, Encoding.UTF8));
                }
            }
        }
    }
}