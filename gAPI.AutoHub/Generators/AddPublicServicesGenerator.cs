namespace gAPI.AutoHub.Generators
{

    internal class AddAutoHubServicesGenerator : BaseGenerator
    {
        internal AddAutoHubServicesGenerator(ServiceContext dataModel, SignalRHubGenerator signalRHub, ClientHandlerGenerator[] clientHandlers, ClientHandlerContextGenerator[] clientHandlerContexts, IHubServiceContextGenerator signalRContext1, HubServiceContextGenerator signalRContext)
        {
            ServiceContext = dataModel;
            SignalRHub = signalRHub;
            ClientHandlers = clientHandlers;
            ClientHandlerContexts = clientHandlerContexts;
            HubServiceContext = signalRContext;

            Directory = dataModel.Config.AddAutoHubServices_Destination.Directory;
            Namespace = dataModel.Config.AddAutoHubServices_Destination.Namespace;

            Name = "AddAutoHubServicesExtention";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext ServiceContext { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerGenerator[] ClientHandlers { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }
        public HubServiceContextGenerator HubServiceContext { get; }

        internal void GenerateCode()
        {
            Reg("Microsoft.Extensions.DependencyInjection");
            var propertiesCode = $"        services.AddScoped<IHubServiceContext, HubServiceContext>();\r\n";
            foreach (var clientHandlerContext in ClientHandlerContexts)
            {
                Reg(clientHandlerContext);
                Reg(clientHandlerContext.IClientHandlerContext);
                propertiesCode += $"        services.AddScoped<{clientHandlerContext.IClientHandlerContext.Name}, {clientHandlerContext.Name}>();\r\n";
            }

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoHubServices(this IServiceCollection services)
    {{
{propertiesCode}    }}
}}";

        }
    }
}