using gAPI.FabricNode;
using gAPI.Ids;

namespace gAPI.FabricNode.Models;

public sealed class InvokeRequestState
{
    public required RequestId RequestId { get; init; }
    public required FabricHost Caller { get; init; }

    public required HashSet<FabricHostId> PendingHosts { get; init; }

    public CancellationTokenSource TimeoutCts { get; } = new();
    public IActor? Actor { get; set; }

    private int _completed;

    public bool TryComplete()
        => Interlocked.Exchange(ref _completed, 1) == 0;
}
