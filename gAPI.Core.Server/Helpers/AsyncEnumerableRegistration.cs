using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Helpers;


public record AsyncEnumerableRegistrationInstance<T>(
    IAsyncEnumerator<T> enumerator,
    SemaphoreSlim gate,
    CancellationTokenSource linkedCts)
{
    public bool IsDisposed { get; private set; }

    public bool TryRemoveFrom(ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> streams, StreamId streamId)
        => ((ICollection<KeyValuePair<StreamId, AsyncEnumerableRegistrationInstance<T>>>)streams)
            .Remove(new(streamId, this));

    public async ValueTask DisposeAsync(bool gateAlreadyHeld = false)
    {
        try
        {
            linkedCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

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
                linkedCts.Dispose();
            }
        }
        finally
        {
            if (!gateAlreadyHeld)
                gate.Release();
        }
    }
}

public interface IAsyncEnumerableRegistration : IAsyncDisposable
{
    Task<StreamingResponseDto> Invoke(StreamId streamId, bool cancelled, CancellationToken ct);
}

public sealed class AsyncEnumerableRegistration<T> : IAsyncEnumerableRegistration
{
    public AsyncEnumerableRegistration(
        Func<ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<StreamingResponseDto>> handler)
    {
        Handler = handler;
    }

    private readonly ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> ActiveStreams = [];
    private readonly Func<ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<StreamingResponseDto>> Handler;

    public Task<StreamingResponseDto> Invoke(StreamId streamId, bool cancelled, CancellationToken ct)
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