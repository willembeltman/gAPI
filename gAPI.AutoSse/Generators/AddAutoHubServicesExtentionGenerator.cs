namespace gAPI.AutoSse.Generators
{

    internal class AddAutoSseServicesExtentionGenerator : BaseGenerator
    {
        internal AddAutoSseServicesExtentionGenerator(ServiceContext dataModel, SignalRHubGenerator signalRHub, ClientHandlerGenerator[] clientHandlers, ClientHandlerContextGenerator[] clientHandlerContexts, ISignalRContextGenerator signalRContext1, SignalRContextGenerator signalRContext)
        {
            ServiceContext = dataModel;
            SignalRHub = signalRHub;
            ClientHandlers = clientHandlers;
            ClientHandlerContexts = clientHandlerContexts;
            SignalRContext = signalRContext;

            Directory = dataModel.Config.AddAutoSseServices_Destination.Directory;
            Namespace = dataModel.Config.AddAutoSseServices_Destination.Namespace;

            Name = "AddAutoSseServicesExtention";
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
            var propertiesCode = $"        services.AddScoped<ISignalRContext, SignalRContext>();\r\n";
            foreach (var clientHandlerContext in ClientHandlerContexts)
            {
                Reg(clientHandlerContext);
                Reg(clientHandlerContext.IClientHandlerContext);
                propertiesCode += $"        services.AddScoped<{clientHandlerContext.IClientHandlerContext.Name}, {clientHandlerContext.Name}>();\r\n";
            }

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoSseServices(this IServiceCollection services)
    {{
{propertiesCode}    }}
}}";

        }
    }
}