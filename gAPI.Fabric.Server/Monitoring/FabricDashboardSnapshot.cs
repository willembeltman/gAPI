namespace gAPI.Fabric.Server.Monitoring;

/// <summary>
/// Immutable operational state that can be rendered by a console, HTTP API, or another UI.
/// </summary>
public sealed record FabricDashboardSnapshot(
    DateTimeOffset CapturedAt,
    int Port,
    IReadOnlyList<FabricConnectionSnapshot> Connections,
    IReadOnlyList<FabricServiceSnapshot> Services);

public sealed record FabricConnectionSnapshot(
    long ConnectionId,
    long SentBytesPerSecond,
    long ReceivedBytesPerSecond);

public sealed record FabricServiceSnapshot(
    string ServiceId,
    long SentBytesPerSecond,
    long ReceivedBytesPerSecond,
    int SessionCount,
    int UserCount);
