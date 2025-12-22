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

            var IHubServiceContext = new IHubServiceContextGenerator(dataModel, SignalRHub, ClientHandlerContexts);
            IHubServiceContext.GenerateCode();
            spc.AddSource(
                Path.Combine(IHubServiceContext.Directory, IHubServiceContext.FileName),
                SourceText.From(IHubServiceContext.Code, Encoding.UTF8));

            var HubServiceContext = new HubServiceContextGenerator(dataModel, SignalRHub, ClientHandlerContexts, IHubServiceContext);
            HubServiceContext.GenerateCode();
            spc.AddSource(
                Path.Combine(HubServiceContext.Directory, HubServiceContext.FileName),
                SourceText.From(HubServiceContext.Code, Encoding.UTF8));

            var AddAutoClientServices = new AddAutoHubServicesGenerator(dataModel, SignalRHub, ClientHandlers, ClientHandlerContexts, IHubServiceContext, HubServiceContext);
            AddAutoClientServices.GenerateCode();
            spc.AddSource(
                Path.Combine(AddAutoClientServices.Directory, AddAutoClientServices.FileName), 
                SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));
        }
    }
}