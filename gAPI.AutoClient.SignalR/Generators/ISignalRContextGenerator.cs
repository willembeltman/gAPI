using System.Linq;

namespace gAPI.AutoHubClient.Generators
{
    internal class ISignalRContextGenerator : BaseGenerator
    {
        internal ISignalRContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerContextGenerator[] clientHandlerContexts)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandlerContexts = clientHandlerContexts;

            Directory = dataModel.Config.HubClients_Destination.Directory;
            Namespace = dataModel.Config.HubClients_Destination.Namespace;

            Name = "ISignalRContext";
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
                        Reg(a.ClientHandler);
                        return $"    {a.ClientHandler.Name} {a.ClientHandler.Interface.ApiName} {{ get; }}";
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