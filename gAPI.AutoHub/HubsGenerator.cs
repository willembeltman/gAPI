using gAPI.AutoHub.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoHub
{
    internal class HubsGenerator
    {
        internal static void Generate(ServiceContext dataModel, SourceProductionContext spc)
        {
            var SignalRHub = new SignalRHubGenerator(dataModel);
            SignalRHub.GenerateCode();
            spc.AddSource(
                Path.Combine(SignalRHub.Directory, SignalRHub.FileName), 
                SourceText.From(SignalRHub.Code, Encoding.UTF8));

            var ClientHandlers = dataModel.Interfaces
                .Select(@interface => new ClientHandlerGenerator(dataModel, @interface))
                .ToArray();

            foreach (var clientHandler in ClientHandlers)
            {
                clientHandler.GenerateCode();
                spc.AddSource(
                    Path.Combine(clientHandler.Directory, clientHandler.FileName),
                    SourceText.From(clientHandler.Code, Encoding.UTF8));
            }

            var IClientHandlerContexts = ClientHandlers
                .Select(clientHandler => new IClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();
            foreach (var iClientHandlerContext in IClientHandlerContexts)
            {
                iClientHandlerContext.GenerateCode();
                spc.AddSource(
                    Path.Combine(iClientHandlerContext.Directory, iClientHandlerContext.FileName), 
                    SourceText.From(iClientHandlerContext.Code, Encoding.UTF8));
            }

            var ClientHandlerContexts = IClientHandlerContexts
                .Select(clientHandler => new ClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();

            foreach (var clientHandlerContext in ClientHandlerContexts)
            {
                clientHandlerContext.GenerateCode();
                spc.AddSource(
                    Path.Combine(clientHandlerContext.Directory, clientHandlerContext.FileName), 
                    SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
            }

            var ISignalRContext = new ISignalRContextGenerator(dataModel, SignalRHub, ClientHandlerContexts);
            ISignalRContext.GenerateCode();
            spc.AddSource(
                Path.Combine(ISignalRContext.Directory, ISignalRContext.FileName),
                SourceText.From(ISignalRContext.Code, Encoding.UTF8));

            var SignalRContext = new SignalRContextGenerator(dataModel, SignalRHub, ClientHandlerContexts, ISignalRContext);
            SignalRContext.GenerateCode();
            spc.AddSource(
                Path.Combine(SignalRContext.Directory, SignalRContext.FileName),
                SourceText.From(SignalRContext.Code, Encoding.UTF8));

            var AddAutoHub = new AddAutoHubExtentionGenerator(dataModel, SignalRHub);
            AddAutoHub.GenerateCode();
            spc.AddSource(
                Path.Combine(AddAutoHub.Directory, AddAutoHub.FileName),
                SourceText.From(AddAutoHub.Code, Encoding.UTF8));

            var AddAutoHubServices = new AddAutoHubServicesExtentionGenerator(dataModel, SignalRHub, ClientHandlers, ClientHandlerContexts, ISignalRContext, SignalRContext);
            AddAutoHubServices.GenerateCode();
            spc.AddSource(
                Path.Combine(AddAutoHubServices.Directory, AddAutoHubServices.FileName),
                SourceText.From(AddAutoHubServices.Code, Encoding.UTF8));
        }
    }
}