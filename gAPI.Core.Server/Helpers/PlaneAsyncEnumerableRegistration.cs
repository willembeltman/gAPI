using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Helpers;


public record PlaneAsyncEnumerableRegistrationInstance<T>(
    IAsyncEnumerator<T> enumerator,
    SemaphoreSlim gate,
    CancellationTokenSource linkedCts)
{
    public bool IsDisposed { get; private set; }

    public bool TryRemoveFrom(ConcurrentDictionary<StreamId, PlaneAsyncEnumerableRegistrationInstance<T>> streams, StreamId streamId)
        => ((ICollection<KeyValuePair<StreamId, PlaneAsyncEnumerableRegistrationInstance<T>>>)streams)
            .Remove(new(streamId, this));

    public async ValueTask DisposeAsync(bool gateAlreadyHeld = false)
    {
        //try
        //{
        //    linkedCts.Cancel();
        //}
        //catch (ObjectDisposedException)
        //{
        //}

        if (!gateAlreadyHeld)
            await gate.WaitAsync();

        try
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            try
            {
                await enumerator.DisposeAsync();
            }
            finally
            {
                //linkedCts.Dispose();
            }
        }
        finally
        {
            if (!gateAlreadyHeld)
                gate.Release();
        }
    }
}

public interface IPlaneAsyncEnumerableRegistration : IAsyncDisposable
{
    Task<PlaneStreamingResponseDto> Invoke(StreamId streamId, bool cancelled, CancellationToken ct);
}

public sealed class PlaneAsyncEnumerableRegistration<T> : IPlaneAsyncEnumerableRegistration
{
    public PlaneAsyncEnumerableRegistration(
        Func<ConcurrentDictionary<StreamId, PlaneAsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<PlaneStreamingResponseDto>> handler)
    {
        Handler = handler;
    }

    private readonly ConcurrentDictionary<StreamId, PlaneAsyncEnumerableRegistrationInstance<T>> ActiveStreams = [];
    private readonly Func<ConcurrentDictionary<StreamId, PlaneAsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<PlaneStreamingResponseDto>> Handler;

    public Task<PlaneStreamingResponseDto> Invoke(StreamId streamId, bool cancelled, CancellationToken ct)
    {
        return Handler.Invoke(ActiveStreams, streamId, cancelled, ct);
    }

    public async ValueTask DisposeAsync()
    {
        while (!ActiveStreams.IsEmpty)
        {
            foreach (var (streamId, stream) in ActiveStreams.ToArray())
            {
                await stream.DisposeAsync();
                stream.TryRemoveFrom(ActiveStreams, streamId);
            }
        }
    }
}