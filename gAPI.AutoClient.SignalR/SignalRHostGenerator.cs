using gAPI.AutoHubClient.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoHubClient
{
    internal class SignalRHostGenerator
    {
        internal static void Generate(ServiceContext dataModel, SourceProductionContext spc)
        {
            var Config = dataModel.Config;

            var SignalRHub = new SignalRHubGenerator(dataModel);
            SignalRHub.GenerateCode();
            var signalRHubFullName = Path.Combine(Config.HubClients_Destination.Directory, SignalRHub.FileName);
            spc.AddSource(signalRHubFullName, SourceText.From(SignalRHub.Code, Encoding.UTF8));

            var ClientHandlers = dataModel.Interfaces
                .Select(@interface => new ClientHandlerGenerator(dataModel, @interface))
                .ToArray();

            foreach (var clientHandler in ClientHandlers)
            {
                clientHandler.GenerateCode();
                var fullName = Path.Combine(Config.HubClients_Destination.Directory, clientHandler.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandler.Code, Encoding.UTF8));
            }

            var IClientHandlerContexts = ClientHandlers
                .Select(clientHandler => new IClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();
            foreach (var clientHandlerContext in IClientHandlerContexts)
            {
                clientHandlerContext.GenerateCode();
                var fullName = Path.Combine(Config.HubClients_Destination.Directory, clientHandlerContext.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
            }

            var ClientHandlerContexts = IClientHandlerContexts
                .Select(clientHandler => new ClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();

            foreach (var clientHandlerContext in ClientHandlerContexts)
            {
                clientHandlerContext.GenerateCode();
                var fullName = Path.Combine(Config.HubClients_Destination.Directory, clientHandlerContext.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
            }

            var SignalRContext = new SignalRContextGenerator(dataModel, SignalRHub, ClientHandlerContexts);
            SignalRContext.GenerateCode();
            var signalRContextFullName = Path.Combine(Config.HubClients_Destination.Directory, SignalRContext.FileName);
            spc.AddSource(signalRContextFullName, SourceText.From(SignalRContext.Code, Encoding.UTF8));

            //AddAutoClientServices = new AddAutoClientServicesGenerator(dataModel);
            //AddAutoClientServices.GenerateCode();
            //var addServicesFullName = Path.Combine(Config.AddAutoClientServices_Destination.Directory, AddAutoClientServices.FileName);
            //spc.AddSource(addServicesFullName, SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));
        }
    }
}