using gAPI.AutoApiClient.Helpers;
using gAPI.AutoApiClient.Models;
using gAPI.AutoSerializer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiClient;

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
                var allSymbols = compilation.GlobalNamespace.GetAllTypes().ToArray();
                var customMultipartFormDataContents = FindCustomSerializer.GetAllCustomMultipartFormDataContents(allSymbols);
                var sharedReferences = new SharedReferences(allSymbols);
                var serviceContext = new ServiceContext(allSymbols);
                var serviceModelErrors = serviceContext.CheckForErrors();
                var generator = new Generator(serviceContext, sharedReferences, customMultipartFormDataContents);
                generator.Generate(spc);

                if (serviceModelErrors.Count > 0)
                {
                    ShowError(string.Join(", ", serviceModelErrors), spc);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.ToString(), spc);
                //throw;
            }
        });
    }

    public void ShowError(Exception exception, SourceProductionContext CurrentSpc)
    {
        ShowError(exception.Message, CurrentSpc);
    }

    public void ShowError(string errorMessage, SourceProductionContext CurrentSpc)
    {
        //throw new Exception(errorMessage); // Helps while debugging
        var sourceCode = $"#error gAPI.AutoApiClient: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
        CurrentSpc.AddSource("Gapi_Error.AutoApiClient.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }


    //        private static void CreateAccessFile(SourceProductionContext spc, DataModel dataModel)
    //        {
    //            var sbEnums = new StringBuilder();
    //            var sbDtos = new StringBuilder();

    //            foreach (var @enum in dataModel.Enums)
    //            {
    //                sbEnums.AppendLine($"            System.Console.WriteLine(@\"{@enum.FullName}\");");
    //            }

    //            foreach (var dto in dataModel.Dtos)
    //            {
    //                sbDtos.AppendLine($"            System.Console.WriteLine(@\"{dto.FullName}\");");
    //            }

    //            var sbInterfaces = new StringBuilder();
    //            foreach (var @enum in dataModel.Interfaces)
    //            {
    //                sbInterfaces.AppendLine($"            System.Console.WriteLine(@\"{@enum.FullName}\");");
    //            }

    //            var sb = $@"
    //namespace gAPI.AutoApiClient
    //{{
    //    public static class AccessedTypes
    //    {{
    //        public static void ConsoleListAll()
    //        {{
    //            System.Console.WriteLine(""Enums:"");
    //{sbEnums}
    //            System.Console.WriteLine(""Dtos:"");
    //{sbDtos}
    //            System.Console.WriteLine(""Interfaces:"");
    //{sbInterfaces}
    //        }}
    //    }}
    //}}";


    //            spc.AddSource("AccessedTypes.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    //        }
}