using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Models;

public sealed class BusRequestState : IDisposable
{
    public required BusRoutingDto Routing { get; init; }
    public required IActor Actor { get; init; }
    public required FabricBusHost Caller { get; init; }
    public required FabricBusHost[] Targets { get; init; }

    public ConcurrentDictionary<FabricBusConnectionId, byte> ReadyTargets { get; } = [];
    public ConcurrentDictionary<FabricBusConnectionId, byte> CompletedTargets { get; } = [];
    public ConcurrentDictionary<StreamId, FabricBusHost> StreamRoutes { get; } = [];
    public ConcurrentDictionary<FabricBusConnectionId, string> Exceptions { get; } = [];

    private int _cancelled;
    public bool Cancelled => Volatile.Read(ref _cancelled) != 0;

    private int _ready;
    private int _completed;

    private ResettableTimeout? Timeout { get; set; }

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
        Timeout = new ResettableTimeout(duration, onTimeout);
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