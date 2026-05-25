using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace gAPI.AutoPage;

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
                var sharedReferences = new SharedReferences(compilation, allSymbols);
                var serviceContext = new ServiceContext(compilation, allSymbols);
                var crudlContext = new CrudlContext(serviceContext);
                var generator = new Generator(sharedReferences, serviceContext, crudlContext, spc);
                generator.Generate();
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
        var sourceCode = $"#error gAPI.AutoPage: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
        CurrentSpc.AddSource("Gapi_Error.AutoPage.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }
}