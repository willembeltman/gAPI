using gAPI.AutoAuth.Server.Generators;
using gAPI.AutoAuth.Server.Generators.Authentication;
using gAPI.AutoAuth.Server.Models;
using gAPI.AutoSerializer;
using gAPI.AutoSerializer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoAuth.Server;

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

        IStateParser = new IStateParserGenerator(this);
        StateParser = new StateParserGenerator(this);

        IAuthenticationService = new IAuthenticationServiceGenerator(this);
        AuthenticationService = new AuthenticationServiceGenerator(this);
        //AuthenticationStateMapping = new AuthenticationStateMappingGenerator(this);
        AddAutoAuthServerExtension = new AddAutoAuthServerExtensionGenerator(this);

        //WssHub = new WssHub_Generator(this);
    }

    public ServiceContext ServiceContext { get; }
    public SharedReferences SharedReferences { get; }

    public CustomObject[] CustomSpanSerializers { get; }
    public CustomObjectMethod[] CustomComparers { get; }
    public CustomObjectMethod[] CustomCreateCopys { get; }
    public CustomObjectMethod[] CustomMultipartFormDataContents { get; }
    public IStateParserGenerator IStateParser { get; }
    public StateParserGenerator StateParser { get; }
    public IAuthenticationServiceGenerator IAuthenticationService { get; }
    public AuthenticationServiceGenerator AuthenticationService { get; }
    //public AuthenticationStateMappingGenerator AuthenticationStateMapping { get; }
    public AddAutoAuthServerExtensionGenerator AddAutoAuthServerExtension { get; }

    public void Generate(SourceProductionContext spc)
    {
        GenerateItem(spc, IStateParser);
        GenerateItem(spc, StateParser);

        GenerateItem(spc, IAuthenticationService);
        GenerateItem(spc, AuthenticationService);
        //GenerateItem(spc, AuthenticationStateMapping);
        GenerateItem(spc, AddAutoAuthServerExtension);

        GenerateSpanSerializers(spc);
        GenerateCreateCopys(spc);
        GenerateComparers(spc);
    }

    private void GenerateSpanSerializers(SourceProductionContext spc)
    {
        var generatedItems = new HashSet<string>();

        //foreach (var api in ClientHandlers)
        //{
        //    var apiSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
        //        api.NeededSerializers.ToArray(),
        //        CustomSpanSerializers.Select(a => a.Type));
        //    foreach (var item in apiSpanSerializers)
        //    {
        //        var name = item.ToDisplayString();
        //        if (generatedItems.Contains(name)) continue;
        //        generatedItems.Add(name);

        //        var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
        //        serializerGenerator.Namespace = api.Namespace!;
        //        var code = serializerGenerator.Generate();
        //        spc.AddSource(
        //            serializerGenerator.FileName,
        //            SourceText.From(code, Encoding.UTF8));
        //    }
        //}

        //var WssHubSpanSerializers = FindAndCreateGenaratorsRecursive.FindAndCreateGenerators(
        //    WssHub.NeededSerializers.ToArray(),
        //    CustomSpanSerializers.Select(a => a.Type));
        //foreach (var item in WssHubSpanSerializers)
        //{
        //    var name = item.ToDisplayString();
        //    if (generatedItems.Contains(name)) continue;
        //    generatedItems.Add(name);

        //    var serializerGenerator = new SpanSerializerGenerator(item, CustomSpanSerializers);
        //    serializerGenerator.Namespace = WssHub.Namespace!;
        //    var code = serializerGenerator.Generate();
        //    spc.AddSource(
        //        serializerGenerator.FileName,
        //        SourceText.From(code, Encoding.UTF8));
        //}

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

    private void GenerateItem(SourceProductionContext spc, _BaseGenerator generator)
    {
        generator.GenerateCode();
        spc.AddSource(
            Path.Combine(generator.Directory, generator.FileName),
            SourceText.From(generator.Code, Encoding.UTF8));
    }
}