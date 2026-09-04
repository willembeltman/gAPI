using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using gAPI.AutoWss.Server.Generators;
using gAPI.AutoWss.Server.Generators.Hubs;
using gAPI.AutoWss.Server.Generators.Endpoints;
using gAPI.AutoWss.Server.Generators.Startup;
using gAPI.AutoWss.Server.Generators.Wss;
using gAPI.AutoWss.Server.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoWss.Server;

public class Generator
{
    public Generator(
        ServiceContext serviceContext,
        SharedReferences sharedReferences,
        CustomObject[] customSpanSerializers,
        CustomObjectMethod[] customComparers,
        CustomObjectMethod[] customCreateCopys,
        CustomObjectMethod[] customMultipartFormDataContents)
    {
        ServiceContext = serviceContext;
        SharedReferences = sharedReferences;
        CustomSpanSerializers = customSpanSerializers;
        CustomComparers = customComparers;
        CustomCreateCopys = customCreateCopys;
        CustomMultipartFormDataContents = customMultipartFormDataContents;

        ServerConnection = new ServerConnection_Generator(this);
        IClientContext = new IClientContext_Generator(this);
        ClientContext = new ClientContext_Generator(this);
        AddAutoWssExtension = new AddAutoWssServerExtensionGenerator(this);
        MapAutoWssExtension = new MapAutoWssServerExtensionGenerator(this);
        MapWssEndpointExtension = new WssEndpointExtensionGenerator(this);

        MinimalApis = serviceContext.MinimalApiInterfaces
            .Select(a => new MinimalApi_Generator(this, a))
            .ToArray();

        ClientHandlers = serviceContext.HubInterfaces
            .Select(@interface => new HubClient_Generator(this, @interface))
            .ToArray();

        IClientHandlerContexts = ClientHandlers
            .Select(clientHandler => new IClientServiceContext_Generator(this, clientHandler))
            .ToArray();

        ClientContexts = IClientHandlerContexts
            .Select(iclientHandler => new ClientServiceContext_Generator(this, iclientHandler))
            .ToArray();


        //IStateParser = new IStateParserGenerator(this);
        //StateParser = new StateParserGenerator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public CustomObjectMethod[] CustomCreateCopys { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContents { get; }


    public ServerConnection_Generator ServerConnection { get; }
    public IClientContext_Generator IClientContext { get; }
    public ClientContext_Generator ClientContext { get; }
    public AddAutoWssServerExtensionGenerator AddAutoWssExtension { get; }
    public MapAutoWssServerExtensionGenerator MapAutoWssExtension { get; }
    public WssEndpointExtensionGenerator MapWssEndpointExtension { get; }
    public MinimalApi_Generator[] MinimalApis { get; }
    public HubClient_Generator[] ClientHandlers { get; }
    public IClientServiceContext_Generator[] IClientHandlerContexts { get; }
    public ClientServiceContext_Generator[] ClientContexts { get; }
    //public IStateParserGenerator IStateParser { get; }
    //public StateParserGenerator StateParser { get; }

    public void Generate(SourceProductionContext spc)
    {
        GenerateItem(spc, ServerConnection);
        GenerateItem(spc, IClientContext);
        GenerateItem(spc, ClientContext);
        GenerateItem(spc, AddAutoWssExtension);
        GenerateItem(spc, MapAutoWssExtension);
        GenerateItem(spc, MapWssEndpointExtension);

        foreach (var item in MinimalApis)
            GenerateItem(spc, item);
        foreach (var item in ClientHandlers)
            GenerateItem(spc, item);
        foreach (var item in IClientHandlerContexts)
            GenerateItem(spc, item);
        foreach (var item in ClientContexts)
            GenerateItem(spc, item);

        //GenerateItem(spc, IStateParser);
        //GenerateItem(spc, StateParser);

        GenerateSpanSerializers(spc);
        GenerateCreateCopys(spc);
        GenerateComparers(spc);
    }

    private void GenerateSpanSerializers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        foreach (var api in ClientHandlers)
        {
            var apiSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
                api.NeededSerializers.ToArray(),
                CustomSpanSerializers.Select(a => a.Type));
            foreach (var item in apiSpanSerializers)
            {
                var name = item.ToDisplayString();
                if (generatedItems.Contains(name)) continue;
                generatedItems.Add(name);

                var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
                serializerGenerator.Namespace = api.Namespace!;
                var code = serializerGenerator.Generate();
                spc.AddSource(
                    serializerGenerator.FileName,
                    SourceText.From(code, Encoding.UTF8));
            }
        }

        var ServerConnectionSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
            ServerConnection.NeededSerializers.ToArray(), 
            CustomSpanSerializers.Select(a => a.Type));
        foreach (var item in ServerConnectionSpanSerializers)
        {
            var name = item.ToDisplayString();
            if (generatedItems.Contains(name)) continue;
            generatedItems.Add(name);

            var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
            serializerGenerator.Namespace = ServerConnection.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
        }

        //var stateParserSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
        //    StateParser.NeededState_ListForBeingLazy.ToArray(),
        //    CustomSpanSerializers.Select(a => a.Type));
        //foreach (var item in stateParserSpanSerializers)
        //{
        //    var name = item.ToDisplayString();
        //    if (generatedItems.Contains(name)) continue;
        //    generatedItems.Add(name);

        //    var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
        //    serializerGenerator.Namespace = StateParser.Namespace!;
        //    var code = serializerGenerator.Generate();
        //    spc.AddSource(
        //        serializerGenerator.FileName,
        //        SourceText.From(code, Encoding.UTF8));
        //}
    }
    private void GenerateCreateCopys(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        //var stateParserSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
        //    StateParser.NeededState_ListForBeingLazy.ToArray(),
        //    CustomCreateCopys.Select(a => a.Type));
        //foreach (var item in stateParserSpanSerializers)
        //{
        //    var name = item.ToDisplayString();
        //    if (generatedItems.Contains(name)) continue;
        //    generatedItems.Add(name);

        //    var serializerGenerator = new CreateCopyGenerator(item, CustomCreateCopys);
        //    serializerGenerator.Namespace = StateParser.Namespace!;
        //    var code = serializerGenerator.Generate();
        //    spc.AddSource(
        //        serializerGenerator.FileName,
        //        SourceText.From(code, Encoding.UTF8));
        //}
    }
    private void GenerateComparers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        //var stateParserSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
        //    StateParser.NeededState_ListForBeingLazy.ToArray(),
        //    CustomComparers.Select(a => a.Type));
        //foreach (var item in stateParserSpanSerializers)
        //{
        //    var name = item.ToDisplayString();
        //    if (generatedItems.Contains(name)) continue;
        //    generatedItems.Add(name);

        //    var serializerGenerator = new ComparerGenerator(item, CustomComparers);
        //    serializerGenerator.Namespace = StateParser.Namespace!;
        //    var code = serializerGenerator.Generate();
        //    spc.AddSource(
        //        serializerGenerator.FileName,
        //        SourceText.From(code, Encoding.UTF8));
        //}
    }

    private void GenerateItem(SourceProductionContext spc, _BaseGenerator generator)
    {
        generator.GenerateCode();
        spc.AddSource(
            Path.Combine(generator.Directory, generator.FileName),
            SourceText.From(generator.Code, Encoding.UTF8));
    }
}