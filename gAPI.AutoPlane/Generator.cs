using gAPI.AutoPlane.Generators;
using gAPI.AutoPlane.Generators.PlaneClient;
using gAPI.AutoPlane.Generators.PlaneClientContext;
using gAPI.AutoPlane.Generators.PlaneConnection;
using gAPI.AutoPlane.Generators.PlaneConnectionHostedService;
using gAPI.AutoPlane.Generators.PlaneContext;
using gAPI.AutoPlane.Generators.Startup;
using gAPI.AutoPlane.Models;
using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoPlane;

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

        IPlaneContext = new IPlaneContext_Generator(this);
        PlaneContext = new PlaneContext_Generator(this);
        HostedPlaneServices = new HostedPlaneServicesGenerator(this);
        IPlaneConnection = new IPlaneConnectionGenerator(this);
        PlaneHostedService = new PlaneConnectionHostedServiceGenerator(this);

        if (SharedReferences.AzureBasePlaneConnection != null)
            PlaneConnection = new PlaneConnectionGenerator(this, "AzureServicePlane", SharedReferences.AzureBasePlaneConnection);
        else if (SharedReferences.RabbitMQBasePlaneConnection != null)
            if (PlaneConnection == null)
                PlaneConnection = new PlaneConnectionGenerator(this, "RabbitMQ", SharedReferences.RabbitMQBasePlaneConnection);
            else
                throw new Exception("You cannot have two service plane providers in the same project");
        if (SharedReferences.BaseFabricConnection != null)
            if (PlaneConnection == null)
                PlaneConnection = new PlaneConnectionGenerator(this, "Fabric", SharedReferences.BaseFabricConnection, overloadFabricClient: true);
            else
                throw new Exception("You cannot have two or more service plane providers in the same project");
        else
            throw new Exception("You must reference one of the base plane projects");


        AddAutoPlaneExtension = new AddAutoPlaneExtensionGenerator(this);

        ClientHandlers = serviceContext.PlaneInterfaces
            .Select(@interface => new PlaneClient_Generator(this, @interface))
            .ToArray();

        IClientHandlerContexts = ClientHandlers
            .Select(clientHandler => new IPlaneClientContext_Generator(this, clientHandler))
            .ToArray();

        ClientContexts = IClientHandlerContexts
            .Select(iclientHandler => new PlaneClientContext_Generator(this, iclientHandler))
            .ToArray();
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public CustomObjectMethod[] CustomCreateCopys { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContents { get; }

    public IPlaneContext_Generator IPlaneContext { get; }
    public PlaneContext_Generator PlaneContext { get; }
    public HostedPlaneServicesGenerator HostedPlaneServices { get; }
    public IPlaneConnectionGenerator IPlaneConnection { get; }
    public PlaneConnectionHostedServiceGenerator PlaneHostedService { get; }
    public AddAutoPlaneExtensionGenerator AddAutoPlaneExtension { get; }
    public PlaneClient_Generator[] ClientHandlers { get; }
    public IPlaneClientContext_Generator[] IClientHandlerContexts { get; }
    public PlaneClientContext_Generator[] ClientContexts { get; }
    public PlaneConnectionGenerator PlaneConnection { get; }

    public void Generate(SourceProductionContext spc)
    {
        GenerateItem(spc, IPlaneContext);
        GenerateItem(spc, PlaneContext);
        GenerateItem(spc, HostedPlaneServices);
        GenerateItem(spc, IPlaneConnection);
        GenerateItem(spc, PlaneConnection);
        GenerateItem(spc, PlaneHostedService);
        GenerateItem(spc, AddAutoPlaneExtension);

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