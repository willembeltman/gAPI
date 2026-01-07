using gAPI.AutoSse.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoSse
{
    internal class SsesGenerator
    {
        internal static void Generate(ServiceContext dataModel, SourceProductionContext spc)
        {
            var SseHostController = new SseHostControllerGenerator(dataModel);
            SseHostController.GenerateCode();
            spc.AddSource(
                Path.Combine(SseHostController.Directory, SseHostController.FileName),
                SourceText.From(SseHostController.Code, Encoding.UTF8));

            var SseServices = dataModel.Interfaces
                .Select(@interface => new SseServiceGenerator(dataModel, @interface))
                .ToArray();

            foreach (var clientHandler in SseServices)
            {
                clientHandler.GenerateCode();
                spc.AddSource(
                    Path.Combine(clientHandler.Directory, clientHandler.FileName),
                    SourceText.From(clientHandler.Code, Encoding.UTF8));
            }

            var ISseServiceContexts = SseServices
                .Select(clientHandler => new ISseServiceContextGenerator(dataModel, clientHandler))
                .ToArray();
            foreach (var iClientHandlerContext in ISseServiceContexts)
            {
                iClientHandlerContext.GenerateCode();
                spc.AddSource(
                    Path.Combine(iClientHandlerContext.Directory, iClientHandlerContext.FileName), 
                    SourceText.From(iClientHandlerContext.Code, Encoding.UTF8));
            }

            var SseServiceContexts = ISseServiceContexts
                .Select(clientHandler => new SseServiceContextGenerator(dataModel, clientHandler))
                .ToArray();

            foreach (var clientHandlerContext in SseServiceContexts)
            {
                clientHandlerContext.GenerateCode();
                spc.AddSource(
                    Path.Combine(clientHandlerContext.Directory, clientHandlerContext.FileName), 
                    SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
            }

            var ISseContext = new ISseContextGenerator(dataModel, SseServiceContexts);
            ISseContext.GenerateCode();
            spc.AddSource(
                Path.Combine(ISseContext.Directory, ISseContext.FileName),
                SourceText.From(ISseContext.Code, Encoding.UTF8));

            var SseContext = new SseContextGenerator(dataModel, SseServiceContexts, ISseContext);
            SseContext.GenerateCode();
            spc.AddSource(
                Path.Combine(SseContext.Directory, SseContext.FileName),
                SourceText.From(SseContext.Code, Encoding.UTF8));

            var AddAutoSseExtention = new AddAutoSseExtentionGenerator(dataModel);
            AddAutoSseExtention.GenerateCode();
            spc.AddSource(
                Path.Combine(AddAutoSseExtention.Directory, AddAutoSseExtention.FileName),
                SourceText.From(AddAutoSseExtention.Code, Encoding.UTF8));
        }
    }
}