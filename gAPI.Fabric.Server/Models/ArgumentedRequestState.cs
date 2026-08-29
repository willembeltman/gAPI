using gAPI.Core.Ids;
using gAPI.Fabric.Server.Interfaces;

namespace gAPI.Fabric.Server.Models;

public sealed class ArgumentedRequestState
{
    public required RequestId RequestId { get; init; }
    public required FabricHost Caller { get; init; }
    public required IActor Actor { get; init; }
    public required HashSet<FabricHostId> Targets { get; init; }
    public HashSet<FabricHostId> CompletedTargets { get; } = [];
    public CancellationTokenSource TimeoutCts { get; } = new();
}
