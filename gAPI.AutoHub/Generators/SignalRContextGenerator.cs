using System.Linq;

namespace gAPI.AutoHub.Generators
{
    internal class HubServiceContextGenerator : BaseGenerator
    {
        internal HubServiceContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerContextGenerator[] clientHandlerContexts,
            IHubServiceContextGenerator iHubServiceContext)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandlerContexts = clientHandlerContexts;
            IHubServiceContext = iHubServiceContext;

            Directory = dataModel.Config.HubServices_Destination.Directory;
            Namespace = dataModel.Config.HubServices_Destination.Namespace;

            Name = "HubServiceContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }
        public IHubServiceContextGenerator IHubServiceContext { get; }

        public void GenerateCode()
        {
            Reg(SignalRHub);
            Reg(IHubServiceContext);
            Reg("Microsoft.AspNetCore.SignalR");
            var properties = string.Join(
                Environment.NewLine,
                ClientHandlerContexts
                    .Select(a =>
                    {
                        Reg(a);
                        Reg(a.IClientHandlerContext);
                        return $"    public {a.IClientHandlerContext.Name} {a.ClientHandler.Interface.ApiName} {{ get; }} = new {a.Name}(hubContext);";
                    }));

            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    IHubContext<SignalRHub> hubContext)
    : {IHubServiceContext.Name}
{{
{properties}
}}
";
        }
    }
}