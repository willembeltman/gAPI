using System.Linq;

namespace gAPI.AutoHub.Generators
{
    internal class SignalRContextGenerator : BaseGenerator
    {
        internal SignalRContextGenerator(
            ServiceContext dataModel,
            SignalRHubGenerator signalRHub,
            ClientHandlerGenerator[] clientHandlers,
            ClientHandlerContextGenerator[] clientHandlerContexts)
        {
            DataModel = dataModel;
            SignalRHub = signalRHub;
            ClientHandlerContexts = clientHandlerContexts;

            Directory = dataModel.Config.Hubs_Destination.Directory;
            Namespace = dataModel.Config.Hubs_Destination.Namespace;

            Name = "SignalRContext";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }

        public void GenerateCode()
        {
            //Reg(ClientHandler);
            Reg("Microsoft.AspNetCore.SignalR");
            Code = @$"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public class {Name}(
    IHubContext<SignalRHub> hubContext)
{{
    {string.Join(
        Environment.NewLine + "    ", 
        ClientHandlerContexts
            .Select(a=> $"public {a.Name} {a.ClientHandler.Interface.ApiName} {{ get; }} = new(hubContext);"))}
}}
";
        }
    }
}