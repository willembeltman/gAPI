using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Models;

public sealed class PlaneInvokeRequestState : IDisposable
{
    public required PlaneRoutingDto Routing { get; init; }
    public required IActor Actor { get; init; }
    public required FabricPlaneHost Caller { get; init; }
    public required ConcurrentDictionary<FabricPlaneConnectionId, FabricPlaneHost> Targets { get; init; }

    public ConcurrentDictionary<FabricPlaneConnectionId, PlaneInvokeRequestAckDto> ReadyTargets { get; } = [];
    public ConcurrentDictionary<FabricPlaneConnectionId, PlaneInvokeRequestDoneDto> CompletedTargets { get; } = [];
    public ConcurrentDictionary<StreamId, FabricPlaneHost> StreamRoutes { get; } = [];

    private int _cancelled;
    public bool Cancelled => Volatile.Read(ref _cancelled) != 0;

    private int _ready;
    private int _completed;

    private CancellationTokenSourceDispose? Timeout { get; set; }

    internal void RemovePlaneHost(FabricPlaneHost host)
    {
        Targets.TryRemove(host.FabricPlaneConnectionId, out _);
        CompletedTargets.TryRemove(host.FabricPlaneConnectionId, out _);

        foreach (var target in StreamRoutes)
        {
            if (target.Value.FabricPlaneConnectionId == host.FabricPlaneConnectionId)
                StreamRoutes.TryRemove(target);
        }
    }
    public void Cancel()
    {
        Volatile.Write(ref _cancelled, 1);
    }

    public bool TryReady()
    {
        if (Interlocked.Exchange(ref _ready, 1) == 0)
        {
            return true;
        }

        return false;
    }

    public bool TryComplete()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 0)
        {
            Dispose();
            return true;
        }

        return false;
    }

    public void StartTimeout(TimeSpan duration, Action onTimeout)
    {
        Timeout = new CancellationTokenSourceDispose(duration);
        Timeout.Register(onTimeout);
    }

    public void ResetTimeout()
    {
        Timeout?.Reset();
    }

    public void Dispose()
    {
        Timeout?.Dispose();
    }

}