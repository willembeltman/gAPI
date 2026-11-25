using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Helpers;


public record BusAsyncEnumerableRegistrationInstance<T>(
    IAsyncEnumerator<T> enumerator,
    SemaphoreSlim gate,
    CancellationTokenSource timeoutCts)
{
    public bool IsDisposed { get; private set; }

    public bool TryRemoveFrom(ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>> streams, StreamId streamId)
        => ((ICollection<KeyValuePair<StreamId, BusAsyncEnumerableRegistrationInstance<T>>>)streams)
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
            catch
            {

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

public record BusAsyncEnumerableRegistrationResponse(BusStreamingResponseDto response, Func<Task>? action);


public interface IBusAsyncEnumerableRegistration : IAsyncDisposable
{
    Task<BusAsyncEnumerableRegistrationResponse> Invoke(StreamId streamId, bool cancelled, CancellationToken ct);
}

public sealed class BusAsyncEnumerableRegistration<T> : IBusAsyncEnumerableRegistration
{
    public BusAsyncEnumerableRegistration(
        Func<ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<BusAsyncEnumerableRegistrationResponse>> handler)
    {
        Handler = handler;
    }

    private readonly ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>> ActiveStreams = [];
    private readonly Func<ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<BusAsyncEnumerableRegistrationResponse>> Handler;

    public Task<BusAsyncEnumerableRegistrationResponse> Invoke(StreamId streamId, bool cancelled, CancellationToken ct)
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