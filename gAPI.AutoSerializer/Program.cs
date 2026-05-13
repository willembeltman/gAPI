using gAPI.AutoSerializer.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

#nullable enable

namespace gAPI.AutoSerializer;

[Generator]
public class Program : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {
            //#if DEBUG
            //            if (!Debugger.IsAttached)
            //            {
            //                Debugger.Launch(); // Triggert dialoog om te attachen
            //            }
            //#endif
            try
            {
                var allSymbols = Helper.GetAllTypes(compilation.GlobalNamespace).ToArray();

                var customSerializers = FindCustomSerializer.GetAllCustomSerializers(allSymbols);
                var customSpanSerializers = FindCustomSerializer.GetAllCustomSpanSerializers(allSymbols);
                var customComparers = FindCustomSerializer.GetAllCustomComparers(allSymbols);
                var customCreateCopys = FindCustomSerializer.GetAllCustomCreateCopys(allSymbols);
                var customMultipartFormDataContents = FindCustomSerializer.GetAllCustomMultipartFormDataContents(allSymbols);

                var binarySerializersToGenerate = FindSerializerToGenerate.GetBinarySerializerToGenerate(allSymbols);

                var serializers = FindAndCreateGenaratorsRecursive
                    .FindAndCreateGenerators(binarySerializersToGenerate, customSerializers.Select(a => a.Type));
                var spanSerializers = FindAndCreateGenaratorsRecursive
                    .FindAndCreateGenerators(binarySerializersToGenerate, customSpanSerializers.Select(a => a.Type));
                var comparers = FindAndCreateGenaratorsRecursive
                    .FindAndCreateGenerators(binarySerializersToGenerate, customComparers.Select(a => a.Type), true);
                var createCopys = FindAndCreateGenaratorsRecursive
                    .FindAndCreateGenerators(binarySerializersToGenerate, customCreateCopys.Select(a => a.Type), true);
                var multipartFormDataContents = FindAndCreateGenaratorsRecursive
                    .FindAndCreateGenerators(binarySerializersToGenerate, customMultipartFormDataContents.Select(a => a.Type));

                foreach (var serializer in serializers)
                {
                    var serializerGenerator = new SerializerGenerator(serializer, customSerializers);
                    var serializerCode = serializerGenerator.Generate();
                    spc.AddSource(
                        serializerGenerator.FileName,
                        SourceText.From(serializerCode, Encoding.UTF8));
                }
                foreach (var spanSerializer in spanSerializers)
                {
                    var ___spanSerializerGenerator = new SpanSerializerGenerator(spanSerializer, customSpanSerializers);
                    var ___spanSerializerCode = ___spanSerializerGenerator.Generate();
                    spc.AddSource(
                        ___spanSerializerGenerator.FileName,
                        SourceText.From(___spanSerializerCode, Encoding.UTF8));
                }
                foreach (var comparer in comparers)
                {
                    var comparerGenerator = new ComparerGenerator(comparer, customComparers);
                    var comparerCode = comparerGenerator.Generate();
                    spc.AddSource(
                        comparerGenerator.FileName,
                        SourceText.From(comparerCode, Encoding.UTF8));
                }
                foreach (var createCopy in createCopys)
                {
                    var createCopyGenerator = new CreateCopyGenerator(createCopy, customCreateCopys);
                    var createCopyCode = createCopyGenerator.Generate();
                    spc.AddSource(
                        createCopyGenerator.FileName,
                        SourceText.From(createCopyCode, Encoding.UTF8));
                }
                foreach (var multipartFormDataContent in multipartFormDataContents)
                {
                    var createCopyGenerator = new MultipartFormDataContentSerializerGenerator(multipartFormDataContent, customMultipartFormDataContents);
                    var createCopyCode = createCopyGenerator.Generate();
                    spc.AddSource(
                        createCopyGenerator.FileName,
                        SourceText.From(createCopyCode, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.ToString(), spc);
                throw;
            }
        });
    }
    public void ShowError(string errorMessage, SourceProductionContext CurrentSpc)
    {
        //throw new Exception(errorMessage); // Helps while debugging
        var sourceCode = $"#error gAPI.AutoSerializer: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
        CurrentSpc.AddSource("Gapi_Error.AutoSerializer.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }
}
