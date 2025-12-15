using gAPI.AutoComponents.Configs;
using gAPI.AutoComponents.Contexts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoComponents
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var configFile = context.AdditionalTextsProvider
                .Where(file => Path.GetFileName(file.Path).Equals("gapisettings.json", StringComparison.OrdinalIgnoreCase))
                .Select((file, ct) => file.GetText(ct)?.ToString())
                .Collect()
                .Select((configs, _) => configs.FirstOrDefault()); // string?

            var combined = context.CompilationProvider.Combine(configFile);

            context.RegisterSourceOutput(combined, (spc, tuple) =>
            {
                var (compilation, configText) = tuple;

                if (string.IsNullOrWhiteSpace(configText))
                {
                    spc.AddSource("Gapi_Error.g.cs", SourceText.From(
                        $"// Config parse error: Config file is empty", Encoding.UTF8));
                    return;
                }

                //#if DEBUG
                //                if (!Debugger.IsAttached)
                //                {
                //                    Debugger.Launch(); // Triggert dialoog om te attachen
                //                }
                //#endif

                try
                {
                    var config = ClientConfigParser.Parse(configText!);
                    var serviceContext = new ServiceContext(compilation, config);
                    var generatedViews = new ViewsGenerator(serviceContext, spc);
                    generatedViews.GenerateViews();
                    //CreateDebugFile(spc, generatedViews);
                }
                catch (Exception ex)
                {
                    ShowError(ex.ToString(), spc);
                }
            });
        }

        private static void CreateDebugFile(SourceProductionContext spc, ViewsGenerator generatedViews)
        {
            var str = "";
            foreach (var crudl in generatedViews.CrudlContext.Crudls)
            {
                str += $"// Crudl: {crudl.Name}\r\n";
                foreach (var method in crudl.Methods)
                {
                    str += $"//    Method: {method.Name} Type: {method.CrudlMethodType}\r\n";
                }
            }
            str += "\r\n// All Crudl Methods:\r\n";
            foreach (var method in generatedViews.CrudlContext.AllCrudlMethods)
            {
                str += $"// Crudl: {method.Type.Name} Method: {method.Name} Type: {method.CrudlMethodType}\r\n";
            }
            spc.AddSource("Debug.g.cs", SourceText.From(str, Encoding.UTF8));
        }

        public void ShowError(string errorMessage, SourceProductionContext CurrentSpc)
        {
            //throw new Exception(errorMessage); // Helps while debugging
            var sourceCode = $"#error gAPI AutoComponents has thrown an error: \\r\\n{errorMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
            CurrentSpc.AddSource("Gapi_Error.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }

        public void ShowWarning(string warningMessage, SourceProductionContext CurrentSpc)
        {
            var sourceCode = $"#warning {warningMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
            CurrentSpc.AddSource("Gapi_Warning.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }
}