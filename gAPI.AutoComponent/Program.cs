using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace gAPI.AutoComponent;

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
        var sourceCode = $"#error gAPI.AutoComponents: {errorMessage.Replace("\r", "").Replace("\n", " ")}";
        CurrentSpc.AddSource("Gapi_Error.AutoComponent.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    //private static void CreateDebugFile(SourceProductionContext spc, ComponentsGenerator generatedViews)
    //{
    //    var str = "";
    //    foreach (var crudl in generatedViews.CrudlContext.Crudls)
    //    {
    //        str += $"// Crudl: {crudl.Name}\r\n";
    //        foreach (var method in crudl.Methods)
    //        {
    //            str += $"//    Method: {method.Name} Type: {method.CrudlMethodType}\r\n";
    //        }
    //    }
    //    str += "\r\n// All Crudl Methods:\r\n";
    //    foreach (var method in generatedViews.CrudlContext.AllCrudlMethods)
    //    {
    //        str += $"// Crudl: {method.Type.Name} Method: {method.Name} Type: {method.CrudlMethodType}\r\n";
    //    }
    //    spc.AddSource("Debug.g.cs", SourceText.From(str, Encoding.UTF8));
    //}

    //public void ShowWarning(string warningMessage, SourceProductionContext CurrentSpc)
    //{
    //    var sourceCode = $"#warning {warningMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
    //    CurrentSpc.AddSource("Gapi_Warning.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    //}
}