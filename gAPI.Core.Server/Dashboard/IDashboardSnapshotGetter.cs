namespace gAPI.Core.Server.Dashboard;

public interface IDashboardSnapshotGetter
{
    Task DisconnectAllAsync();
    FabricDashboardSnapshot GetDashboardSnapshot();
}