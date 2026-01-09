using System.Linq;
using System;
using gAPI.AutoHub.Models;

namespace gAPI.AutoHub.Generators;

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

        Directory = dataModel.Config.HubServices_Destination.Directory;
        Namespace = dataModel.Config.HubServices_Destination.Namespace;

        Name = ClientHandler.Interface.ApiName + "Context";
        FileName = $"{Name}.g.cs";
    }

    public ServiceContext DataModel { get; }
    public SignalRHubGenerator SignalRHub { get; }
    public IClientHandlerContextGenerator IClientHandlerContext { get; }
    public ClientHandlerGenerator ClientHandler { get; }
    public Interface IClientHandler { get; }

    public void GenerateCode()
    {
        Reg(SignalRHub);
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
    public {IClientHandler.Name} ToAll
        => new {ClientHandler.Name}(hubContext.Clients.All);
    public {IClientHandler.Name} ToUser(object userId)
        => new {ClientHandler.Name}(hubContext.Clients.Group(userId.ToString()!));
}}
";
    }
}