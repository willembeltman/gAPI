//using gAPI.Core.Dtos;
//using gAPI.Core.Helpers;
//using gAPI.Core.Ids;
//using gAPI.Fabric.Server.Interfaces;
//using gAPI.Fabric.Server.Services;

//namespace gAPI.Fabric.Server.Models;

//public sealed class SendRequestState : IDisposable
//{
//    public required RoutingDto Routing { get; init; }
//    public required IActor Actor { get; init; }
//    public required FabricHost Caller { get; init; }
//    public required FabricHost[] Targets { get; init; }

//    public HashSet<FabricConnectionId> CompletedTargets { get; } = [];
//    public Dictionary<FabricConnectionId, string> Exceptions { get; } = [];
//    public bool Cancelled { get; set; }
//    private ResettableTimeout? Timeout { get; set; }

//    private int _completed;
//    public bool TryComplete()
//    {
//        if (Interlocked.Exchange(ref _completed, 1) == 0)
//        {
//            Timeout?.Dispose();
//            return true;
//        }
//        return false;
//    }

//    public void StartTimeout(TimeSpan duration, Action onTimeout)
//    {
//        Timeout = new ResettableTimeout(duration, onTimeout);
//    }

//    public void ResetTimeout()
//    {
//        Timeout?.Reset();
//    }

//    public void Dispose()
//    {
//        Timeout?.Dispose();
//    }
//}