using System.Linq;

namespace gAPI.AutoHub.Generators
{
    internal class SignalRContextGenerator : BaseGenerator
    {
        internal SignalRContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerContextGenerator[] clientHandlerContexts,
            ISignalRContextGenerator iSignalRContext)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandlerContexts = clientHandlerContexts;
            ISignalRContext = iSignalRContext;

            Directory = dataModel.Config.Hubs_Destination.Directory;
            Namespace = dataModel.Config.Hubs_Destination.Namespace;

            Name = "SignalRContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }
        public ISignalRContextGenerator ISignalRContext { get; }

        public void GenerateCode()
        {
            Reg(ISignalRContext);
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
    : {ISignalRContext.Name}
{{
{properties}
}}
";
        }
    }
}