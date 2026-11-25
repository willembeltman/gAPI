using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Client.Helpers;


public record AsyncEnumerableRegistrationInstance<T>(
    IAsyncEnumerator<T> enumerator,
    SemaphoreSlim gate,
    CancellationTokenSource timeoutCts)
{
    public bool IsDisposed { get; private set; }

    public bool TryRemoveFrom(ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> streams, StreamId streamId)
        => ((ICollection<KeyValuePair<StreamId, AsyncEnumerableRegistrationInstance<T>>>)streams)
            .Remove(new(streamId, this));

    public async ValueTask DisposeAsync(bool gateAlreadyHeld = false)
    {
        try
        {
            timeoutCts.Cancel();
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
                timeoutCts.Dispose();
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
    Task<AsyncEnumerableRegistrationResponse> Invoke(StreamId streamId, bool cancelled, CancellationToken ct);
}

public record AsyncEnumerableRegistrationResponse(StreamingResponseClientDto response, Func<Task>? action);

public sealed class AsyncEnumerableRegistration<T> : IAsyncEnumerableRegistration
{
    public AsyncEnumerableRegistration(
        Func<ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<AsyncEnumerableRegistrationResponse>> handler)
    {
        Handler = handler;
    }

    private readonly ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>> ActiveStreams = [];
    private readonly Func<ConcurrentDictionary<StreamId, AsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<AsyncEnumerableRegistrationResponse>> Handler;

    public Task<AsyncEnumerableRegistrationResponse> Invoke(StreamId streamId, bool cancelled, CancellationToken ct)
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