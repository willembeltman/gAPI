//using gAPI.Core.Ids;
//using gAPI.Fabric.Server.Interfaces;
//using gAPI.Fabric.Server.Services;

//namespace gAPI.Fabric.Server.Models;

//public sealed class SendRequestState
//{
//    public required RequestId RequestId { get; init; }
//    public required FabricHost Caller { get; init; }

//    public required HashSet<FabricHostId> Targets { get; init; }
//    public HashSet<FabricHostId> CompletedTargets { get; } = [];
//    public Dictionary<FabricHostId, string> Exceptions { get; } = [];

//    public CancellationTokenSource TimeoutCts { get; } = new();
//    public required IActor Actor { get; init; }

//    private int _completed;
//    public bool TryComplete()
//        => Interlocked.Exchange(ref _completed, 1) == 0;
//}
