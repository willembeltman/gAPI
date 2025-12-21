using gAPI.AutoClient.Configs;
using gAPI.AutoClient.Contexts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoClient
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
                    var config = ClientConfigParser.Parse(configText);
                    var dataModel = new ServiceContext(compilation, config);
                    var generatedDataModel = new ClientsGenerator(dataModel, spc);
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
            var sourceCode = $"#error gAPI AutoComponents has thrown an error: \\r\\n{errorMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
            CurrentSpc.AddSource("Gapi_Error.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
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
        //namespace gAPI.AutoClient
        //{{
        //    internal static class AccessedTypes
        //    {{
        //        internal static void ConsoleListAll()
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
}