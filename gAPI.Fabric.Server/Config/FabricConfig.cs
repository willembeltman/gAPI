using gAPI.Core.Server.Dashboard;
using Microsoft.Extensions.Logging;

namespace gAPI.Fabric.Server.Config;

public class FabricConfig : IDashboardName
{
    public int Port { get; set; } = 9494;
    public int BusPort { get; set; } = 9595;
    public LogLevel LogLevel { get; set; }

    public string DashboardName => $"Port {Port} / BusPort {BusPort} / LogLevel {LogLevel}";
}