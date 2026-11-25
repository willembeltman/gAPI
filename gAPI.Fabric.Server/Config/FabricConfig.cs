using gAPI.Core.Server.Dashboard;
using Microsoft.Extensions.Logging;

namespace gAPI.Fabric.Server.Config;

public class FabricConfig : IDashboardName
{
    public int? MaxPackageSize { get; set; }
    public int Port { get; set; } = 30494;
    public int BusPort { get; set; } = 30595;
    public int PlanePort { get; set; } = 30696;
    public LogLevel LogLevel { get; set; }

    public string DashboardName => $"Port {Port} / BusPort {PlanePort} / PlanePort {PlanePort} / LogLevel {LogLevel}";
}