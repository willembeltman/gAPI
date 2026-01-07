using System.Linq;
using System;
using gAPI.AutoSse.Models;

namespace gAPI.AutoSse.Generators
{
    internal class SseServiceContextGenerator : BaseGenerator
    {
        internal SseServiceContextGenerator(
            ServiceContext dataModel,
            ISseServiceContextGenerator iClientHandlerContext)
        {
            DataModel = dataModel;
            IClientHandlerContext = iClientHandlerContext;
            ClientHandler = IClientHandlerContext.ClientHandler;
            IClientHandler = ClientHandler.Interface; 

            Directory = dataModel.Config.SseServices_Destination.Directory;
            Namespace = dataModel.Config.SseServices_Destination.Namespace;

            Name = ClientHandler.Interface.ApiName + "Context";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public ISseServiceContextGenerator IClientHandlerContext { get; }
        public SseServiceGenerator ClientHandler { get; }
        public Interface IClientHandler { get; }

        public void GenerateCode()
        {
            Code = "";
            return;
            Reg(IClientHandlerContext);
            Reg(IClientHandler);
            Reg(ClientHandler);
            Reg("Microsoft.AspNetCore.SignalR");
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    ISseContext<SignalRSse> sseContext)
    : {IClientHandlerContext.Name}
{{
    public {IClientHandler.Name} ToAll
        => new {ClientHandler.Name}(sseContext.Clients.All);
    public {IClientHandler.Name} ToUser(object userId)
        => new {ClientHandler.Name}(sseContext.Clients.Group(userId.ToString()!));
}}
";
        }
    }
}