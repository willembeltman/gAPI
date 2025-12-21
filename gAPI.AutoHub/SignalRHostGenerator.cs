using gAPI.AutoHub.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoHub
{
    internal class SignalRHostGenerator
    {
        internal static void Generate(ServiceContext dataModel, SourceProductionContext spc)
        {
            var Config = dataModel.Config;

            var SignalRHub = new SignalRHubGenerator(dataModel);
            SignalRHub.GenerateCode();
            var signalRHubFullName = Path.Combine(Config.Hubs_Destination.Directory, SignalRHub.FileName);
            spc.AddSource(signalRHubFullName, SourceText.From(SignalRHub.Code, Encoding.UTF8));

            var ClientHandlers = dataModel.Interfaces
                .Select(@interface => new ClientHandlerGenerator(dataModel, @interface))
                .ToArray();

            foreach (var clientHandler in ClientHandlers)
            {
                clientHandler.GenerateCode();
                var fullName = Path.Combine(Config.Hubs_Destination.Directory, clientHandler.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandler.Code, Encoding.UTF8));
            }

            var IClientHandlerContexts = ClientHandlers
                .Select(clientHandler => new IClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();
            foreach (var iClientHandlerContext in IClientHandlerContexts)
            {
                iClientHandlerContext.GenerateCode();
                var fullName = Path.Combine(Config.Hubs_Destination.Directory, iClientHandlerContext.FileName);
                spc.AddSource(fullName, SourceText.From(iClientHandlerContext.Code, Encoding.UTF8));
            }

            var ClientHandlerContexts = IClientHandlerContexts
                .Select(clientHandler => new ClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();

            foreach (var clientHandlerContext in ClientHandlerContexts)
            {
                clientHandlerContext.GenerateCode();
                var fullName = Path.Combine(Config.Hubs_Destination.Directory, clientHandlerContext.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
            }

            var ISignalRContext = new ISignalRContextGenerator(dataModel, SignalRHub, ClientHandlerContexts);
            ISignalRContext.GenerateCode();
            var iSignalRContextFullName = Path.Combine(Config.Hubs_Destination.Directory, ISignalRContext.FileName);
            spc.AddSource(iSignalRContextFullName, SourceText.From(ISignalRContext.Code, Encoding.UTF8));

            var SignalRContext = new SignalRContextGenerator(dataModel, SignalRHub, ClientHandlerContexts, ISignalRContext);
            SignalRContext.GenerateCode();
            var signalRContextFullName = Path.Combine(Config.Hubs_Destination.Directory, SignalRContext.FileName);
            spc.AddSource(signalRContextFullName, SourceText.From(SignalRContext.Code, Encoding.UTF8));

            var AddAutoClientServices = new AddAutoHubServicesGenerator(dataModel, SignalRHub, ClientHandlers, ClientHandlerContexts, ISignalRContext, SignalRContext);
            AddAutoClientServices.GenerateCode();
            var addServicesFullName = Path.Combine(Config.AddAutoHubServices_Destination.Directory, AddAutoClientServices.FileName);
            spc.AddSource(addServicesFullName, SourceText.From(AddAutoClientServices.Code, Encoding.UTF8));
        }
    }
}