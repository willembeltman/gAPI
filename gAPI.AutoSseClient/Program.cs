using gAPI.AutoSseClient.Helpers;
using gAPI.AutoSseClient.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace gAPI.AutoSseClient;

[Generator]
public class Program : IIncrementalGenerator
{
    public SourceProductionContext CurrentSpc { get; private set; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
        {
            //#if DEBUG
            //                if (!Debugger.IsAttached)
            //                {
            //                    Debugger.Launch(); // Triggert dialoog om te attachen
            //                }
            //#endif

            try
            {
                var allSymbols = compilation.GlobalNamespace.GetAllTypes().ToArray();
                var sharedReferences = new SharedReferences(allSymbols);
                var serviceContext = new ServiceContext(allSymbols);
                var serviceModelErrors = serviceContext.CheckForErrors();
                var generator = new Generator(serviceContext, sharedReferences);
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
        var sourceCode = $"#error gAPI.AutoSseClient: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
        CurrentSpc.AddSource("Gapi_Error.AutoSseClient.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    //public void ShowError(Exception exception)
    //{
    //    ShowError(exception.Message);
    //}

    //public void ShowError(string errorMessage)
    //{
    //    //throw new Exception(errorMessage); // Helps while debugging
    //    var sourceCode = $"#error {errorMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
    //    CurrentSpc.AddSource("Gapi_Error.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    //}

    //public void ShowWarning(string warningMessage)
    //{
    //    var sourceCode = $"#warning {warningMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
    //    CurrentSpc.AddSource("Gapi_Warning.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    //}

    //        private static void CreateAccessFile(SourceProductionContext spc, ServiceContext dataModel)
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
    //            var sbServices = new StringBuilder();

    //            foreach (var @enum in dataModel.Interfaces)
    //            {
    //                sbInterfaces.AppendLine($"            System.Console.WriteLine(@\"{@enum.FullName}\");");
    //            }

    //            foreach (var dto in dataModel.ClientHandlers)
    //            {
    //                sbServices.AppendLine($"            System.Console.WriteLine(@\"{dto.FullName}\");");
    //            }

    //            var sb = $@"
    //namespace GeneratorTypes
    //{{
    //    public static class AllControllers
    //    {{
    //        public static void ListAll()
    //        {{
    //            System.Console.WriteLine(""Enums:"");
    //{sbEnums}
    //            System.Console.WriteLine(""Dtos:"");
    //{sbDtos}
    //            System.Console.WriteLine(""Interfaces:"");
    //{sbInterfaces}
    //            System.Console.WriteLine(""Services:"");
    //{sbServices}
    //        }}
    //    }}
    //}}";


    //            spc.AddSource("GeneratedTypes.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    //        }
}