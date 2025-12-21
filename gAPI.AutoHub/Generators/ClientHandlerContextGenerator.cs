using System.Linq;
using System;
using gAPI.AutoHub.Models;

namespace gAPI.AutoHub.Generators
{
    internal class ClientHandlerContextGenerator : BaseGenerator
    {
        internal ClientHandlerContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            IClientHandlerContextGenerator iClientHandlerContext)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            IClientHandlerContext = iClientHandlerContext;
            ClientHandler = IClientHandlerContext.ClientHandler;
            IClientHandler = ClientHandler.Interface; 

            Directory = dataModel.Config.Hubs_Destination.Directory;
            Namespace = dataModel.Config.Hubs_Destination.Namespace;

            Name = ClientHandler.Interface.ApiName + "ClientHandlerContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public IClientHandlerContextGenerator IClientHandlerContext { get; }
        public ClientHandlerGenerator ClientHandler { get; }
        public Interface IClientHandler { get; }

        public void GenerateCode()
        {
            Reg(IClientHandlerContext);
            Reg(IClientHandler);
            Reg(ClientHandler);
            Reg("Microsoft.AspNetCore.SignalR");
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    IHubContext<SignalRHub> hubContext)
    : {IClientHandlerContext.Name}
{{
    public {IClientHandler.Name} All
        => new {ClientHandler.Name}(hubContext.Clients.All);
    public {IClientHandler.Name} ByUserId(object userId)
        => new {ClientHandler.Name}(hubContext.Clients.Group(userId.ToString()));
}}
";
        }
    }
}