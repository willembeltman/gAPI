namespace gAPI.AutoHub.Generators
{

    internal class AddAutoHubServicesGenerator : BaseGenerator
    {
        internal AddAutoHubServicesGenerator(ServiceContext dataModel, SignalRHubGenerator signalRHub, ClientHandlerGenerator[] clientHandlers, ClientHandlerContextGenerator[] clientHandlerContexts, SignalRContextGenerator signalRContext)
        {
            ServiceContext = dataModel;
            SignalRHub = signalRHub;
            ClientHandlers = clientHandlers;
            ClientHandlerContexts = clientHandlerContexts;
            SignalRContext = signalRContext;

            Directory = dataModel.Config.AddAutoHubServices_Destination.Directory;
            Namespace = dataModel.Config.AddAutoHubServices_Destination.Namespace;

            Name = "AddAutoHubServicesExtention";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext ServiceContext { get; }
        public SignalRHubGenerator SignalRHub { get; }
        public ClientHandlerGenerator[] ClientHandlers { get; }
        public ClientHandlerContextGenerator[] ClientHandlerContexts { get; }
        public SignalRContextGenerator SignalRContext { get; }

        internal void GenerateCode()
        {
            Reg("Microsoft.Extensions.DependencyInjection");
            var propertiesCode = $"        services.AddScoped<SignalRContext>();\r\n";
            foreach (var service in ClientHandlerContexts)
            {
                Reg(service.Namespace);
                propertiesCode += $"        services.AddScoped<{service.Name}>();\r\n";
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