using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.ServiceBus.Helpers;

public record BusAsyncEnumerableRegistrationInstance<T>(
    IAsyncEnumerator<T> enumerator,
    SemaphoreSlim gate,
    CancellationTokenSource linkedCts);

public interface IBusAsyncEnumerableRegistration : IAsyncDisposable
{
    Task<BusStreamingResponseDto> Invoke(StreamId streamId, bool cancelled, CancellationToken ct);
}

public sealed class BusAsyncEnumerableRegistration<T> : IBusAsyncEnumerableRegistration
{
    public BusAsyncEnumerableRegistration(
        Func<ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<BusStreamingResponseDto>> handler)
    {
        Handler = handler;
    }

    private readonly ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>> ActiveStreams = [];
    private readonly Func<ConcurrentDictionary<StreamId, BusAsyncEnumerableRegistrationInstance<T>>, StreamId, bool, CancellationToken, Task<BusStreamingResponseDto>> Handler;

    public Task<BusStreamingResponseDto> Invoke(StreamId streamId, bool cancelled, CancellationToken ct)
    {
        return Handler.Invoke(ActiveStreams, streamId, cancelled, ct);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var stream in ActiveStreams.Values)
        {
            await stream.enumerator.DisposeAsync();
            stream.gate.Dispose();
            stream.linkedCts.Dispose();
        }

        ActiveStreams.Clear();
    }
}