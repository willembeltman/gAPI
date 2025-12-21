using System.Linq;
using System;

namespace gAPI.AutoHub.Generators
{
    internal class ClientHandlerContextGenerator : BaseGenerator
    {
        internal ClientHandlerContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerGenerator clientHandler)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandler = clientHandler;

            Directory = dataModel.Config.Hubs_Destination.Directory;
            Namespace = dataModel.Config.Hubs_Destination.Namespace;

            Name = ClientHandler.Interface.ApiName + "ClientHandlerContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerGenerator ClientHandler { get; }

        public void GenerateCode()
        {
            Reg(ClientHandler);
            Reg("Microsoft.AspNetCore.SignalR");
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    IHubContext<SignalRHub> hubContext)
{{
    public {ClientHandler.Name} All
        => new {ClientHandler.Name}(hubContext.Clients.All);
    public {ClientHandler.Name} ByUserId(object userId)
        => new {ClientHandler.Name}(hubContext.Clients.Group(userId.ToString()));
}}
";
        }
    }
}