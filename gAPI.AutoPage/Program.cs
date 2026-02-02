using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Models;
using gAPI.AutoPage.Models.Configs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoPage;

[Generator]
public class Program : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configFile = context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path).Equals("gapi.autopage.json", StringComparison.OrdinalIgnoreCase))
            .Select((file, ct) => file.GetText(ct)?.ToString())
            .Collect()
            .Select((configs, _) => configs.FirstOrDefault()); // string?
         
        var combined = context.CompilationProvider.Combine(configFile);

        context.RegisterSourceOutput(combined, (spc, tuple) =>
        {
            var (compilation, configText) = tuple;

            if (string.IsNullOrWhiteSpace(configText))
            {
                ShowError($"Config parse error: Config file is empty", spc);
                return;
            }

//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch(); // Triggert dialoog om te attachen
//            }
//#endif
             
            //try
            //{
                var config = PageConfigParser.Parse(configText!);
                var allSymbols = compilation.GlobalNamespace.GetAllTypes().ToArray();
                var sharedReferences = new SharedReferences(compilation, allSymbols);
                var serviceContext = new ServiceContext(compilation, config, allSymbols);
                var crudlContext = new CrudlContext(serviceContext);
                var generatedViews = new Generator(config, sharedReferences, serviceContext, crudlContext, spc);
                generatedViews.GenerateViews();
            //}
            //catch (Exception ex)
            //{
            //    ShowError(ex.ToString(), spc);
            //    //throw;
            //}
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