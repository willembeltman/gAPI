namespace gAPI.AutoSse.Generators
{
    internal class AddAutoSseExtentionGenerator : BaseGenerator
    {
        internal AddAutoSseExtentionGenerator(ServiceContext serviceContext, SignalRHubGenerator signalRHub)
        {
            ServiceContext = serviceContext;
            SignalRHub = signalRHub;

            Directory = serviceContext.Config.AddAutoSseServices_Destination.Directory;
            Namespace = serviceContext.Config.AddAutoSseServices_Destination.Namespace;

            Name = "AddAutoSseExtention";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext ServiceContext { get; }
        public SignalRHubGenerator SignalRHub { get; }

        internal void GenerateCode()
        {
            Reg(SignalRHub);
            Reg("Microsoft.Extensions.DependencyInjection");
            Reg("Microsoft.AspNetCore.Routing");
            Reg("Microsoft.AspNetCore.Builder");
            Reg("System.Reflection");

            Code = $@"{GetNamespacesCode()}namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoSse(this IServiceCollection services)
    {{
        services.AddSignalR();
        services.AddAutoSseServices();
    }}

    public static void MapAutoSse(this IEndpointRouteBuilder app)
    {{
        app.MapHub<{SignalRHub.Name}>(""/hubs/signalrhub"");
    }}
}}
";

        }
    }
}