using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using gAPI.AutoWssServer.Generators;
using gAPI.AutoWssServer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoWssServer;

public class Generator
{
    public Generator(
        ServiceContext serviceContext,
        SharedReferences sharedReferences,
        CustomObject[] customSerializers,
        CustomObject[] customSpanSerializers,
        CustomObjectMethod[] customComparers)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;
        CustomSerializers = customSerializers;
        CustomSpanSerializers = customSpanSerializers;
        CustomComparers = customComparers;

        WssHub = new WssHub_Generator(this);
        IClientContext = new IClientContext_Generator(this);
        ClientContext = new ClientContext_Generator(this);
        AddAutoWssExtension = new AutoWssExtensionGenerator(this);

        MinimalApis = serviceContext.MinimalApiInterfaces
            .Select(a => new MinimalApi_Generator(this, a))
            .ToArray();

        ClientHandlers = serviceContext.HubInterfaces
            .Select(@interface => new ClientService_Generator(this, @interface))
            .ToArray();

        IClientHandlerContexts = ClientHandlers
            .Select(clientHandler => new IClientServiceContext_Generator(this, clientHandler))
            .ToArray();

        ClientContexts = IClientHandlerContexts
            .Select(iclientHandler => new ClientServiceContext_Generator(this, iclientHandler))
            .ToArray();

        AddAutoWssServicesExtension = new AutoWssServicesExtensionGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public CustomObject[] CustomSerializers { get; }
    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public WssHub_Generator WssHub { get; }
    public IClientContext_Generator IClientContext { get; }
    public ClientContext_Generator ClientContext { get; }
    public AutoWssExtensionGenerator AddAutoWssExtension { get; }
    public AutoWssServicesExtensionGenerator AddAutoWssServicesExtension { get; }
    public MinimalApi_Generator[] MinimalApis { get; }
    public ClientService_Generator[] ClientHandlers { get; }
    public IClientServiceContext_Generator[] IClientHandlerContexts { get; }
    public ClientServiceContext_Generator[] ClientContexts { get; }

    public void Generate(SourceProductionContext spc)
    {
        GenerateItem(spc, WssHub);
        GenerateItem(spc, IClientContext);
        GenerateItem(spc, ClientContext);
        GenerateItem(spc, AddAutoWssExtension);
        GenerateItem(spc, AddAutoWssServicesExtension);

        foreach (var item in MinimalApis)
            GenerateItem(spc, item);
        foreach (var item in ClientHandlers)
            GenerateItem(spc, item);
        foreach (var item in IClientHandlerContexts)
            GenerateItem(spc, item);
        foreach (var item in ClientContexts)
            GenerateItem(spc, item);

        foreach (var api in ClientHandlers)
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

        var items2 = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(WssHub.NeededSerializers.ToArray(), CustomSpanSerializers.Select(a => a.Type));
        foreach (var item in items2)
        {
            var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
            serializerGenerator.Namespace = WssHub.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private void GenerateItem(SourceProductionContext spc, _BaseGenerator generator)
    {
        generator.GenerateCode();
        spc.AddSource(
            Path.Combine(generator.Directory, generator.FileName),
            SourceText.From(generator.Code, Encoding.UTF8));
    }
}