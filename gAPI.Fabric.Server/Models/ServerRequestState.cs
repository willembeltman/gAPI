using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Models;

public sealed class ServerRequestState : IDisposable
{
    public required ServerRoutingDto Routing { get; init; }
    public required IActor Actor { get; init; }
    public required FabricHost Caller { get; init; }
    public required FabricHost[] Targets { get; init; }

    public ConcurrentDictionary<FabricConnectionId, byte> ReadyTargets { get; } = [];
    public ConcurrentDictionary<FabricConnectionId, byte> CompletedTargets { get; } = [];
    public ConcurrentDictionary<StreamId, FabricHost> StreamRoutes { get; } = [];
    public ConcurrentDictionary<FabricConnectionId, string> Exceptions { get; } = [];

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