using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSse.Generators;

internal class AddAutoSseExtentionGenerator : BaseGenerator
{
    internal AddAutoSseExtentionGenerator(
        ServiceContext serviceContext,
        IEnumerable<SseServiceGenerator> sseServices,
        ISseContextGenerator iSseContext, 
        SseContextGenerator sseContext)
    {
        ServiceContext = serviceContext;
        SseServices = sseServices;
        ISseContext = iSseContext;
        SseContext = sseContext;

        Directory = serviceContext.Config.AddAutoSseServices_Destination.Directory;
        Namespace = serviceContext.Config.AddAutoSseServices_Destination.Namespace;

        Name = "AddAutoSseExtention";
        FileName = $"{Name}.g.cs";
    }

    public ServiceContext ServiceContext { get; }
    public IEnumerable<SseServiceGenerator> SseServices { get; }
    public ISseContextGenerator ISseContext { get; }
    public SseContextGenerator SseContext { get; }

    internal void GenerateCode()
    {
        //Reg("gAPI.Fabric");
        //Reg("gAPI.Sse");
        //Reg("BSD.Core.SseContexts");
        //Reg("BSD.Core.SseServices");
        Reg(ISseContext);
        Reg(SseContext);
        Reg(ServiceContext.SseHostCollection);
        Reg(ServiceContext.FabricClient);
        Reg("Microsoft.Extensions.DependencyInjection");

        var services = string.Join(Environment.NewLine, SseServices.Select(s =>
        {
            var i = s.Interface;
            Reg(i);
            Reg(s);
            return $@"
        services.AddScoped<{i.Name}, {s.Name}>();";
        }));

        Code = $@"{GetNamespacesCode()}#nullable enable

namespace {Namespace};

public static class {Name}
{{
    public static void AddAutoSse(this IServiceCollection services, string? server, int? port = 9494)
    {{
        var fabricClient =
            server == null || port == null
            ? new {ServiceContext.FabricClient}()                     // Don't use a fabricNode
            : new {ServiceContext.FabricClient}(server, port.Value);  // Use the settings
        _ = Task.Run(fabricClient.ConnectAsync);
        var sseHostCollection = new {ServiceContext.SseHostCollection}();
        services.AddSingleton(fabricClient);
        services.AddSingleton(sseHostCollection);
        services.AddScoped<{ISseContext}, {SseContext}>();
    }}
}}
";

    }
}