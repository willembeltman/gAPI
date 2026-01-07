namespace gAPI.AutoSse.Generators
{
    internal class AddAutoSseExtentionGenerator : BaseGenerator
    {
        internal AddAutoSseExtentionGenerator(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;

            Directory = serviceContext.Config.AddAutoSseServices_Destination.Directory;
            Namespace = serviceContext.Config.AddAutoSseServices_Destination.Namespace;

            Name = "AddAutoSseExtention";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext ServiceContext { get; }

        internal void GenerateCode()
        {
            //using gAPI.Fabric;
            //using gAPI.Sse;
            //using Microsoft.Extensions.DependencyInjection;
            Reg("gAPI.Fabric");
            Reg("gAPI.Sse");
            Reg("Microsoft.Extensions.DependencyInjection");

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoSse(this IServiceCollection services, string? server, int? port = 9494)
    {{
        var fabricClient =
            server == null
            ? new FabricClient()                     // Don't use a fabricNode
            : new FabricClient(server, port.Value);  // Use the settings
        _ = Task.Run(fabricClient.ConnectAsync);
        var sseHostCollection = new SseHostCollection();
        services.AddSingleton(fabricClient);
        services.AddSingleton(sseHostCollection);
        services.AddScoped<ISseContext, SseContext>();
        services.AddScoped<ITestClientServiceContext, TestClientServiceContext>();
    }}
}}
";

        }
    }
}