using gAPI.AutoApiSse.Client.Generators;
using gAPI.AutoApiSse.Client.Generators.Authentication;
using gAPI.AutoApiSse.Client.Generators.Clients;
using gAPI.AutoApiSse.Client.Generators.Sse;
using gAPI.AutoApiSse.Client.Generators.Startup;
using gAPI.AutoApiSse.Client.Models;
using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiSse.Client;

public class Generator
{
    public Generator(
        ServiceContext serviceContext, 
        SharedReferences sharedReferences,
        CustomObjectMethod[] customMultipartFormDataContentSerializers)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;
        CustomMultipartFormDataContentSerializers = customMultipartFormDataContentSerializers;

        FormFile = new FormFileGenerator(this);
        IsFormFileExtension = new FormFileExtensionGenerator(this);
        Clients = ServiceContext.ApiInterfaces
            .Concat(ServiceContext.MinimalApiInterfaces)
            .Select(service => new ApiClientGenerator(this, service, customMultipartFormDataContentSerializers))
            .ToArray();
        AddAutoClientServices = new AddAutoApiSseClientExtensionGenerator(this);

        ClientConnection = new ClientConnectionGenerator(this);
        IClientConnection = new IClientConnectionGenerator(this);

        ClientConnection = new ClientConnectionGenerator(this);
        IClientConnection = new IClientConnectionGenerator(this);

        StateParser = new StateParserGenerator(this);
        IAuthenticatedHttpClient = new IAuthenticatedHttpClientGenerator(this);
        AuthenticatedHttpClient = new AuthenticatedHttpClientGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContentSerializers { get; }

    public FormFileGenerator FormFile { get; }
    public FormFileExtensionGenerator IsFormFileExtension { get; }
    public ApiClientGenerator[] Clients { get; }
    public AddAutoApiSseClientExtensionGenerator AddAutoClientServices { get; }

    public ClientConnectionGenerator ClientConnection { get; }
    public IClientConnectionGenerator IClientConnection { get; }

    public StateParserGenerator StateParser { get; }
    public IAuthenticatedHttpClientGenerator IAuthenticatedHttpClient { get; }
    public AuthenticatedHttpClientGenerator AuthenticatedHttpClient { get; }

    public void Generate(SourceProductionContext spc)
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


        ClientConnection.GenerateCode();
        spc.AddSource(Path.Combine(ClientConnection.Directory, ClientConnection.FileName), SourceText.From(ClientConnection.Code, Encoding.UTF8));

        IClientConnection.GenerateCode();
        spc.AddSource(Path.Combine(IClientConnection.Directory, IClientConnection.FileName), SourceText.From(IClientConnection.Code, Encoding.UTF8));

        GenerateItem(spc, StateParser);

        if (SharedReferences.IClientAuthenticatedHttpClientImplementation == null)
        {
            GenerateItem(spc, IAuthenticatedHttpClient);
            GenerateItem(spc, AuthenticatedHttpClient);
        }
    }

    private static void GenerateItem(SourceProductionContext spc, _BaseGenerator generator)
    {
        generator.GenerateCode();

        if (!string.IsNullOrEmpty(generator.Code))
        {
            var signalRHubFullName = Path.Combine(generator.Directory, generator.FileName);
            spc.AddSource(signalRHubFullName, SourceText.From(generator.Code, Encoding.UTF8));
        }
    }
}