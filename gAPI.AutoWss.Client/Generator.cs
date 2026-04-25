using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using gAPI.AutoWssClient.Generators;
using gAPI.AutoWssClient.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoWssClient;

public class Generator
{
    public Generator(
        ServiceContext serviceContext,
        SharedReferences sharedReferences,
        CustomObject[] customSerializers,
        CustomObject[] customSpanSerializers,
        CustomObjectMethod[] customComparers,
        CustomObjectMethod[] customMultipartFormDataContentSerializers)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;
        CustomSerializers = customSerializers;
        CustomSpanSerializers = customSpanSerializers;
        CustomComparers = customComparers;
        CustomMultipartFormDataContentSerializers = customMultipartFormDataContentSerializers;

        Apis = ServiceContext.ApiInterfaces.Select(a => new ApiClientGenerator(this, a)).ToArray();
        MinimalApis = ServiceContext.MinimalApiInterfaces.Select(a => new MinimalClientGenerator(this, a, customMultipartFormDataContentSerializers)).ToArray();
        IClientConnection = new IClientConnectionGenerator(this);
        ClientConnection = new ClientConnectionGenerator(this);
        AddAutoWssExtension = new AutoWssExtensionGenerator(this);
        FormFile = new FormFileGenerator(this);
        FormFileExtension = new FormFileExtensionGenerator(this);
    }
    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public CustomObject[] CustomSerializers { get; }
    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContentSerializers { get; }
    public ApiClientGenerator[] Apis { get; }
    public MinimalClientGenerator[] MinimalApis { get; }
    public IClientConnectionGenerator IClientConnection { get; }
    public ClientConnectionGenerator ClientConnection { get; }
    public AutoWssExtensionGenerator AddAutoWssExtension { get; }
    public FormFileGenerator FormFile { get; }
    public FormFileExtensionGenerator FormFileExtension { get; }

    public void Generate(SourceProductionContext spc)
    {
        foreach (var api in Apis)
            Generate2(spc, api);
        foreach (var api in MinimalApis)
            Generate2(spc, api);
        Generate2(spc, IClientConnection);
        Generate2(spc, ClientConnection);
        Generate2(spc, AddAutoWssExtension);
        Generate2(spc, FormFile);
        Generate2(spc, FormFileExtension);

        foreach (var api in Apis)
        {
            var items = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(api.NeededSerializers.ToArray(), CustomSpanSerializers.Select(a => a.Type));
            foreach (var item in items)
            {
                var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
                serializerGenerator.Namespace = api.Namespace!;
                var code = serializerGenerator.Generate();
                spc.AddSource(
                    serializerGenerator.FileName,
                    SourceText.From(code, Encoding.UTF8));
            }
        }

        foreach (var api in MinimalApis)
        {
            var items = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(api.NeededSerializers.ToArray(), CustomMultipartFormDataContentSerializers.Select(a => a.Type));
            foreach (var item in items)
            {
                var serializerGenerator = new MultipartFormDataContentSerializerGenerator(item, CustomMultipartFormDataContentSerializers);
                serializerGenerator.Namespace = api.Namespace!;
                var code = serializerGenerator.Generate();
                spc.AddSource(
                    serializerGenerator.FileName,
                    SourceText.From(code, Encoding.UTF8));
            }
        }

        var items2 = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(ClientConnection.NeededSerializers.ToArray(), CustomSpanSerializers.Select(a => a.Type));
        foreach (var item in items2)
        {
            var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
            serializerGenerator.Namespace = ClientConnection.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private static void Generate2(SourceProductionContext spc, BaseGenerator generator)
    {
        generator.GenerateCode();

        if (!string.IsNullOrEmpty(generator.Code))
        {
            var signalRHubFullName = Path.Combine(generator.Directory, generator.FileName);
            spc.AddSource(signalRHubFullName, SourceText.From(generator.Code, Encoding.UTF8));
        }
    }
}