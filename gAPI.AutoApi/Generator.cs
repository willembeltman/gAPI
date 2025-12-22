using gAPI.AutoApi.Configs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApi
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        public SourceProductionContext CurrentSpc { get; private set; }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var configFile = context.AdditionalTextsProvider
                .Where(file => Path.GetFileName(file.Path).Equals("gapi.autoapi.json", StringComparison.OrdinalIgnoreCase))
                .Select((file, ct) => file.GetText(ct)?.ToString())
                .Collect()
                .Select((configs, _) => configs.FirstOrDefault()); // string?

            var combined = context.CompilationProvider.Combine(configFile);

            context.RegisterSourceOutput(combined, (spc, tuple) =>
            {
                var (compilation, configText) = tuple;

                CurrentSpc = spc;

                if (string.IsNullOrWhiteSpace(configText))
                {
                    ShowError($"Config parse error: Config file is empty", spc);
                    return;
                }

                //#if DEBUG
                //                    if (!Debugger.IsAttached)
                //                    {
                //                        Debugger.Launch(); // Triggert dialoog om te attachen
                //                    }
                //#endif

                try
                {
                    var config = ServerConfigParser.Parse(configText);
                    var dataModel = new ServiceContext(compilation, config);
                    var apisGenerator = new ApisGenerator(dataModel, spc);
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

        //        public void ShowWarning(string warningMessage)
        //        {
        //            var sourceCode = $"#warning {warningMessage.Replace("\r", "\\r").Replace("\n", "\\n")}";
        //            CurrentSpc.AddSource("Gapi_Warning.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        //        }

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

        //            foreach (var dto in dataModel.Services)
        //            {
        //                sbServices.AppendLine($"            System.Console.WriteLine(@\"{dto.FullName}\");");
        //            }

        //            var sb = $@"
        //namespace GeneratorTypes
        //{{
        //    internal static class AllControllers
        //    {{
        //        internal static void ListAll()
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
}