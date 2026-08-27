using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using gAPI.AutoAuth.Client.Generators;
using gAPI.AutoAuth.Client.Generators.Authentication;
using gAPI.AutoAuth.Client.Generators.Startup;
using gAPI.AutoAuth.Client.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoAuth.Client;

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

        AddAutoAuthExtension = new AddAutoAuthClientExtensionGenerator(this);

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
    public AddAutoAuthClientExtensionGenerator AddAutoAuthExtension { get; }
    public IStateParserGenerator IStateParser { get; }
    public StateParserGenerator StateParser { get; }
    public IAuthenticatedHttpClientGenerator IAuthenticatedHttpClient { get; }
    public AuthenticatedHttpClientGenerator AuthenticatedHttpClient { get; }

    public void Generate(SourceProductionContext spc)
    {
        GenerateItem(spc, AddAutoAuthExtension);
        GenerateItem(spc, IStateParser);
        GenerateItem(spc, StateParser);

        if (SharedReferences.IClientAuthenticatedHttpClientImplementation == null)
        {
            GenerateItem(spc, IAuthenticatedHttpClient);
            GenerateItem(spc, AuthenticatedHttpClient);
        }

        GenerateSpanSerializers(spc);
        GenerateCreateCopys(spc);
        GenerateComparers(spc);

    }

    private void GenerateSpanSerializers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

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