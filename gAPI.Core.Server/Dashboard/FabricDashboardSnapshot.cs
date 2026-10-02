namespace gAPI.Core.Server.Dashboard;

/// <summary>
/// Immutable operational state that can be rendered by a console, HTTP API, or another UI.
/// </summary>
public sealed record FabricDashboardSnapshot(
    DateTimeOffset CapturedAt,
    IReadOnlyList<FabricConnectionSnapshot> Connections,
    IReadOnlyList<FabricServiceSnapshot> Services);

public sealed record FabricConnectionSnapshot(
    string ConnectionId,
    long SentBytesPerSecond,
    long ReceivedBytesPerSecond,
    int SentCount,
    int ReceivedCount);

public sealed record FabricServiceSnapshot(
    string ServiceId,
    long SentBytesPerSecond,
    long ReceivedBytesPerSecond,
    int SentCount,
    int ReceivedCount,
    int SessionCount,
    int UserCount);
