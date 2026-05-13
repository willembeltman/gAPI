using gAPI.AutoApiServer.Helpers;
using gAPI.AutoApiServer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace gAPI.AutoApiServer;

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
                ShowError(ex, spc);
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
        var sourceCode = $"#error gAPI.AutoApi: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
        CurrentSpc.AddSource("Gapi_Error.AutoApi.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }
}