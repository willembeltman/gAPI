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
                var crudContext = new CrudContext(serviceContext);
                var generator = new Generator(sharedReferences, serviceContext, crudContext, spc);
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
    //    foreach (var crud in generatedViews.CrudContext.Cruds)
    //    {
    //        str += $"// Crud: {crud.Name}\r\n";
    //        foreach (var method in crud.Methods)
    //        {
    //            str += $"//    Method: {method.Name} Type: {method.CrudMethodType}\r\n";
    //        }
    //    }
    //    str += "\r\n// All Crud Methods:\r\n";
    //    foreach (var method in generatedViews.CrudContext.AllCrudMethods)
    //    {
    //        str += $"// Crud: {method.Type.Name} Method: {method.Name} Type: {method.CrudMethodType}\r\n";
    //    }
    //    spc.AddSource("Debug.g.cs", SourceText.From(str, Encoding.UTF8));
    //}

    //public void ShowWarning(string warningMessage, SourceProductionContext CurrentSpc)
    //{
    //    var sourceCode = $"#warning {warningMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
    //    CurrentSpc.AddSource("Gapi_Warning.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    //}
}