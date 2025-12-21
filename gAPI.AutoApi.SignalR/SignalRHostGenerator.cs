using gAPI.AutoApi.SignalR.Configs;
using gAPI.AutoApi.SignalR.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace gAPI.AutoApi.SignalR
{
    internal class SignalRHostGenerator
    {
        public SignalRHostGenerator(ServiceContext dataModel, SourceProductionContext spc)
        {
            Config = dataModel.Config;

            SignalRHub = new SignalRHubGenerator(dataModel);
            SignalRHub.GenerateCode();
            var signalRHubFullName = Path.Combine(Config.Hubs_Destination.Directory, SignalRHub.FileName);
            spc.AddSource(signalRHubFullName, SourceText.From(SignalRHub.Code, Encoding.UTF8));

            ClientHandlers = dataModel.Interfaces
                .Select(@interface => new ClientHandlerGenerator(dataModel, @interface))
                .ToArray();

            foreach (var clientHandler in ClientHandlers)
            {
                clientHandler.GenerateCode();
                var fullName = Path.Combine(Config.Hubs_Destination.Directory, clientHandler.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandler.Code, Encoding.UTF8));
            }

            ClientHandlerContexts = ClientHandlers
                .Select(clientHandler => new ClientHandlerContextGenerator(dataModel, SignalRHub, clientHandler))
                .ToArray();

            foreach (var clientHandlerContext in ClientHandlerContexts)
            {
                clientHandlerContext.GenerateCode();
                var fullName = Path.Combine(Config.Hubs_Destination.Directory, clientHandlerContext.FileName);
                spc.AddSource(fullName, SourceText.From(clientHandlerContext.Code, Encoding.UTF8));
            }

            SignalRContext = new SignalRContextGenerator(dataModel, SignalRHub, ClientHandlers, ClientHandlerContexts);
            SignalRContext.GenerateCode();
            var signalRContextFullName = Path.Combine(Config.Hubs_Destination.Directory, SignalRContext.FileName);
            spc.AddSource(signalRContextFullName, SourceText.From(SignalRContext.Code, Encoding.UTF8));

            AddAutoApiServices = new AddAutoHubServicesGenerator(dataModel, SignalRHub, ClientHandlers, ClientHandlerContexts, SignalRContext);
            AddAutoApiServices.GenerateCode();
            var addServicesFullName = Path.Combine(Config.AddAutoHubServices_Destination.Directory, AddAutoApiServices.FileName);
            spc.AddSource(addServicesFullName, SourceText.From(AddAutoApiServices.Code, Encoding.UTF8));
        }

        public ServerConfig Config { get; private set; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerGenerator[] ClientHandlers { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }
        public SignalRContextGenerator SignalRContext { get; }
        public AddAutoHubServicesGenerator AddAutoApiServices { get; }
    }
}