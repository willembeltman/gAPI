using gAPI.AutoBus.Generators;
using gAPI.AutoBus.Generators.BusClient;
using gAPI.AutoBus.Generators.BusClientContext;
using gAPI.AutoBus.Generators.BusConnection;
using gAPI.AutoBus.Generators.BusContext;
using gAPI.AutoBus.Generators.BusHostedService;
using gAPI.AutoBus.Generators.Startup;
using gAPI.AutoBus.Models;
using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoBus;

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

        IBusContext = new IBusContext_Generator(this);
        BusContext = new BusContext_Generator(this);
        IBusConnection = new IBusConnectionGenerator(this);
        BusHostedService = new BusHostedServiceGenerator(this);

        if (SharedReferences.BaseAzureConnection != null)
            BusConnection = new BusConnectionGenerator(this, "AzureServiceBus", SharedReferences.BaseAzureConnection);

        if (SharedReferences.BaseRabbitMQConnection != null)
            if (BusConnection == null)
                BusConnection = new BusConnectionGenerator(this, "RabbitMQ", SharedReferences.BaseRabbitMQConnection);
            else
                throw new Exception("You cannot have two service bus providers in the same project");
        if (SharedReferences.BaseFabricConnection != null)
            if (BusConnection == null)
                BusConnection = new BusConnectionGenerator(this, "Fabric", SharedReferences.BaseFabricConnection, overloadFabricClient: true);
            else
                throw new Exception("You cannot have two or more service bus providers in the same project");

        AddAutoBusExtension = new AddAutoBusExtensionGenerator(this);

        ClientHandlers = serviceContext.BusInterfaces
            .Select(@interface => new BusClient_Generator(this, @interface))
            .ToArray();

        IClientHandlerContexts = ClientHandlers
            .Select(clientHandler => new IBusClientContext_Generator(this, clientHandler))
            .ToArray();

        ClientContexts = IClientHandlerContexts
            .Select(iclientHandler => new BusClientContext_Generator(this, iclientHandler))
            .ToArray();
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public CustomObjectMethod[] CustomCreateCopys { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContents { get; }

    public IBusContext_Generator IBusContext { get; }
    public BusContext_Generator BusContext { get; }
    //public BusConnectionGenerator? FabricConnection { get; }
    //public BusConnectionGenerator? RabbitMQConnection { get; }
    //public BusConnectionGenerator? AzureConnection { get; }
    public IBusConnectionGenerator IBusConnection { get; }
    public BusHostedServiceGenerator BusHostedService { get; }
    public AddAutoBusExtensionGenerator AddAutoBusExtension { get; }
    public BusClient_Generator[] ClientHandlers { get; }
    public IBusClientContext_Generator[] IClientHandlerContexts { get; }
    public BusClientContext_Generator[] ClientContexts { get; }
    public BusConnectionGenerator BusConnection { get; }

    public void Generate(SourceProductionContext spc)
    {
        GenerateItem(spc, IBusContext);
        GenerateItem(spc, BusContext);
        GenerateItem(spc, IBusConnection);
        GenerateItem(spc, BusConnection);
        GenerateItem(spc, BusHostedService);

        //if (FabricConnection != null)
        //    GenerateItem(spc, FabricConnection);
        //if (RabbitMQConnection != null)
        //    GenerateItem(spc, RabbitMQConnection);
        //if (AzureConnection != null)
        //    GenerateItem(spc, AzureConnection);
        GenerateItem(spc, AddAutoBusExtension);

        foreach (var item in ClientHandlers)
            GenerateItem(spc, item);
        foreach (var item in IClientHandlerContexts)
            GenerateItem(spc, item);
        foreach (var item in ClientContexts)
            GenerateItem(spc, item);

        GenerateSpanSerializers(spc);
        GenerateCreateCopys(spc);
        GenerateComparers(spc);
    }

    private void GenerateSpanSerializers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();
        var fileNames = new HashSet<string>();

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

                if (fileNames.Contains(serializerGenerator.FileName)) continue;
                fileNames.Add(serializerGenerator.FileName);

                spc.AddSource(
                    serializerGenerator.FileName,
                    SourceText.From(code, Encoding.UTF8));
            }
        }

        //var ServerConnectionSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
        //    ServerConnection.NeededSerializers.ToArray(),
        //    CustomSpanSerializers.Select(a => a.Type));
        //foreach (var item in ServerConnectionSpanSerializers)
        //{
        //    var name = item.ToDisplayString();
        //    if (generatedItems.Contains(name)) continue;
        //    generatedItems.Add(name);

        //    var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
        //    serializerGenerator.Namespace = ServerConnection.Namespace!;
        //    var code = serializerGenerator.Generate();

        //    if (fileNames.Contains(serializerGenerator.FileName)) continue;
        //    fileNames.Add(serializerGenerator.FileName);

        //    spc.AddSource(
        //        serializerGenerator.FileName,
        //        SourceText.From(code, Encoding.UTF8));
        //}

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