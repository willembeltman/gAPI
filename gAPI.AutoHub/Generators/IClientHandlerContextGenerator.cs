using System.Linq;
using System;
using gAPI.AutoHub.Models;

namespace gAPI.AutoHub.Generators;

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
        IClientHandler = ClientHandler.Interface;

        Directory = dataModel.Config.HubServices_Destination.Directory;
        Namespace = dataModel.Config.HubServices_Destination.Namespace;

        Name = "I" + ClientHandler.Interface.ApiName + "Context";
        FileName = $"{Name}.g.cs";
    }

    public ServiceContext DataModel { get; }
    public SignalRHubGenerator SignalRHub { get; }
    public ClientHandlerGenerator ClientHandler { get; }
    public Interface IClientHandler { get; }

    public void GenerateCode()
    {
        Reg(IClientHandler);
        Reg(ClientHandler);
        Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public interface {Name}
{{
    {IClientHandler.Name} ToAll {{ get; }}
    {IClientHandler.Name} ToUser(object userId);
}}
";
    }
}