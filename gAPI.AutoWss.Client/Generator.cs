using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using gAPI.AutoWss.Client.Generators;
using gAPI.AutoWss.Client.Generators.Authentication;
using gAPI.AutoWss.Client.Generators.Clients;
using gAPI.AutoWss.Client.Generators.Startup;
using gAPI.AutoWss.Client.Generators.Wss;
using gAPI.AutoWss.Client.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoWss.Client;

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

        Apis = ServiceContext.ApiInterfaces.Select(a => new ApiClientGenerator(this, a)).ToArray();
        MinimalApis = ServiceContext.MinimalApiInterfaces.Select(a => new MinimalClientGenerator(this, a, customMultipartFormDataContents)).ToArray();
        IClientConnection = new IClientConnectionGenerator(this);
        ClientConnection = new ClientConnectionGenerator(this);
        AddAutoWssExtension = new AddAutoWssClientExtensionGenerator(this);
        FormFile = new FormFileGenerator(this);
        FormFileExtension = new FormFileExtensionGenerator(this);

        IStateParser = new IStateParserGenerator(this);
        StateParser = new StateParserGenerator(this);
        IAuthenticatedHttpClient = new IAuthenticatedHttpClientGenerator(this);
        AuthenticatedHttpClient = new AuthenticatedHttpClientGenerator(this);
    }
    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }
    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public CustomObjectMethod[] CustomCreateCopys { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContents { get; }
    public ApiClientGenerator[] Apis { get; }
    public MinimalClientGenerator[] MinimalApis { get; }
    public IClientConnectionGenerator IClientConnection { get; }
    public ClientConnectionGenerator ClientConnection { get; }
    public AddAutoWssClientExtensionGenerator AddAutoWssExtension { get; }
    public FormFileGenerator FormFile { get; }
    public FormFileExtensionGenerator FormFileExtension { get; }
    public IStateParserGenerator IStateParser { get; }
    public StateParserGenerator StateParser { get; }
    public IAuthenticatedHttpClientGenerator IAuthenticatedHttpClient { get; }
    public AuthenticatedHttpClientGenerator AuthenticatedHttpClient { get; }

    public void Generate(SourceProductionContext spc)
    {
        foreach (var api in Apis)
            GenerateItem(spc, api);
        foreach (var api in MinimalApis)
            GenerateItem(spc, api);
        GenerateItem(spc, IClientConnection);
        GenerateItem(spc, ClientConnection);
        GenerateItem(spc, AddAutoWssExtension);
        GenerateItem(spc, FormFile);
        GenerateItem(spc, FormFileExtension);

        GenerateItem(spc, IStateParser);
        GenerateItem(spc, StateParser);

        if (SharedReferences.IClientAuthenticatedHttpClientImplementation == null)
        {
            GenerateItem(spc, IAuthenticatedHttpClient);
            GenerateItem(spc, AuthenticatedHttpClient);
        }

        GenerateSpanSerializers(spc);
        GenerateMultipartSerializers(spc);
        GenerateCreateCopys(spc);
        GenerateComparers(spc);

    }

    private void GenerateSpanSerializers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        foreach (var api in Apis)
        {
            var items = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
                api.NeededSpanSerializerTypes.ToArray(),
                CustomSpanSerializers.Select(a => a.Type));

            foreach (var item in items)
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

        var clientConnectionSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
            ClientConnection.NeededSpanSerializers.ToArray(),
            CustomSpanSerializers.Select(a => a.Type));
        foreach (var item in clientConnectionSpanSerializers)
        {
            var name = item.ToDisplayString();
            if (generatedItems.Contains(name)) continue;
            generatedItems.Add(name);

            var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
            serializerGenerator.Namespace = ClientConnection.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
        }

        var stateParserSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
            StateParser.NeededState_ListForBeingLazy.ToArray(),
            CustomSpanSerializers.Select(a => a.Type));
        foreach (var item in stateParserSpanSerializers)
        {
            var name = item.ToDisplayString();
            if (generatedItems.Contains(name)) continue;
            generatedItems.Add(name);

            var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
            serializerGenerator.Namespace = StateParser.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
        }
    }
    private void GenerateMultipartSerializers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        foreach (var api in MinimalApis)
        {
            var items = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
                api.NeededMultipartFormSerializerTypes.ToArray(),
                CustomMultipartFormDataContents.Select(a => a.Type));
            foreach (var item in items)
            {
                var name = item.ToDisplayString();
                if (generatedItems.Contains(name)) continue;
                generatedItems.Add(name);

                var serializerGenerator = new MultipartFormDataContentSerializerGenerator(
                    item,
                    CustomMultipartFormDataContents);
                serializerGenerator.Namespace = api.Namespace!;
                var code = serializerGenerator.Generate();
                spc.AddSource(
                    serializerGenerator.FileName,
                    SourceText.From(code, Encoding.UTF8));
            }
        }
    }
    private void GenerateCreateCopys(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        var stateParserSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
            StateParser.NeededState_ListForBeingLazy.ToArray(),
            CustomCreateCopys.Select(a => a.Type));
        foreach (var item in stateParserSpanSerializers)
        {
            var name = item.ToDisplayString();
            if (generatedItems.Contains(name)) continue;
            generatedItems.Add(name);

            var serializerGenerator = new CreateCopyGenerator(item, CustomCreateCopys);
            serializerGenerator.Namespace = StateParser.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
        }
    }
    private void GenerateComparers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        var stateParserSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
            StateParser.NeededState_ListForBeingLazy.ToArray(),
            CustomComparers.Select(a => a.Type));
        foreach (var item in stateParserSpanSerializers)
        {
            var name = item.ToDisplayString();
            if (generatedItems.Contains(name)) continue;
            generatedItems.Add(name);

            var serializerGenerator = new ComparerGenerator(item, CustomComparers);
            serializerGenerator.Namespace = StateParser.Namespace!;
            var code = serializerGenerator.Generate();
            spc.AddSource(
                serializerGenerator.FileName,
                SourceText.From(code, Encoding.UTF8));
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