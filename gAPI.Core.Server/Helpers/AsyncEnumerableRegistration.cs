using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.Collections.Concurrent;

namespace gAPI.Core.Server.Helpers;


public record AsyncEnumerableRegistrationInstance<T>(
    IAsyncEnumerator<T> enumerator,
    SemaphoreSlim gate,
    CancellationTokenSource linkedCts);

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
        foreach (var stream in ActiveStreams.Values)
        {
            await stream.enumerator.DisposeAsync();
            stream.gate.Dispose();
            stream.linkedCts.Dispose();
        }

        ActiveStreams.Clear();
    }
}