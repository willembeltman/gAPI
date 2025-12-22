using System.Linq;

namespace gAPI.AutoHub.Generators
{
    internal class IHubServiceContextGenerator : BaseGenerator
    {
        internal IHubServiceContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerContextGenerator[] clientHandlerContexts)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandlerContexts = clientHandlerContexts;

            Directory = dataModel.Config.HubServices_Destination.Directory;
            Namespace = dataModel.Config.HubServices_Destination.Namespace;

            Name = "IHubServiceContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }

        public void GenerateCode()
        {
            var properties = string.Join(
                Environment.NewLine,
                ClientHandlerContexts
                    .Select(a =>
                    {
                        Reg(a.IClientHandlerContext);
                        return $"    {a.IClientHandlerContext.Name} {a.ClientHandler.Interface.ApiName} {{ get; }}";
                    }));
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public interface {Name}
{{
{properties}
}}
";
        }
    }
}