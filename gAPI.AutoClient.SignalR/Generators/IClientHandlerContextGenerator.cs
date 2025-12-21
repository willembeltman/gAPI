using System.Linq;
using System;

namespace gAPI.AutoHubClient.Generators
{
    internal class IClientHandlerContextGenerator : BaseGenerator
    {
        internal IClientHandlerContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerGenerator clientHandler)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandler = clientHandler;

            Directory = dataModel.Config.HubClients_Destination.Directory;
            Namespace = dataModel.Config.HubClients_Destination.Namespace;

            Name = "I" + ClientHandler.Interface.ApiName + "ClientHandlerContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerGenerator ClientHandler { get; }

        public void GenerateCode()
        {
            Reg(ClientHandler);
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public interface {Name}
{{
    {ClientHandler.Name} All {{ get; }}
    {ClientHandler.Name} ByUserId(object userId);
}}
";
        }
    }
}