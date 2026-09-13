using gAPI.Core.Ids;
using System.Threading.Channels;

namespace gAPI.Core.Helpers;

public sealed class RemoteAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Action<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken> _create;
    private readonly Func<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken, Task> _requestNext;
    private readonly Func<StreamId, IRemoteAsyncEnumerator<T>, Task> _cancelled;
    private readonly Action<StreamId> _dispose;

    public RemoteAsyncEnumerable(
        Action<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken> construct,
        Func<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken, Task> requestNext,
        Func<StreamId, IRemoteAsyncEnumerator<T>, Task> cancelled,
        Action<StreamId> dispose)
    {
        _create = construct;
        _requestNext = requestNext;
        _cancelled = cancelled;
        _dispose = dispose;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Enumerator(
            _create,
            _requestNext,
            _cancelled,
            _dispose,
            cancellationToken);
    }

    private sealed class Enumerator : IAsyncEnumerator<T>, IRemoteAsyncEnumerator<T>
    {
        private readonly StreamId _streamId = StreamId.New();
        private readonly Channel<T> _items = Channel.CreateUnbounded<T>();

        private readonly Func<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken, Task> _requestNext;
        private readonly Func<StreamId, IRemoteAsyncEnumerator<T>, Task> _cancelled;
        private readonly Action<StreamId> _dispose;
        private readonly CancellationToken _cancellationToken;
        private readonly ResettableTimeout _timeout;

        private int _cancelSent;
        private int _completed;

        public Enumerator(
            Action<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken> create,
            Func<StreamId, IRemoteAsyncEnumerator<T>, CancellationToken, Task> requestNext,
            Func<StreamId, IRemoteAsyncEnumerator<T>, Task> cancelled,
            Action<StreamId> dispose,
            CancellationToken cancellationToken)
        {
            _requestNext = requestNext;
            _cancelled = cancelled;
            _dispose = dispose;
            _cancellationToken = cancellationToken;

            _timeout = new ResettableTimeout(
                TimeSpan.FromSeconds(60),
                () => Complete(
                    new TimeoutException(
                        "Remote async enumerable timed out.")));

            create(_streamId, this, cancellationToken);
        }

        public T Current { get; private set; } = default!;

        public void Push(T item)
        {
            if (Volatile.Read(ref _completed) != 0)
                return;

            if (_items.Writer.TryWrite(item))
                _timeout.Reset();
        }

        public void Complete(Exception? error = null)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0)
                return;

            _timeout.Dispose();
            _items.Writer.TryComplete(error);
        }

        private async Task CancelRemoteAsync()
        {
            if (Interlocked.Exchange(ref _cancelSent, 1) != 0)
                return;

            try
            {
                await _cancelled(_streamId, this);
            }
            catch
            {
                // The consumer is already being cancelled.
                // The local stream still needs to be completed.
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                await CancelRemoteAsync();

                Complete(new OperationCanceledException(_cancellationToken));
                throw new OperationCanceledException(_cancellationToken);
            }

            try
            {
                await _requestNext(
                    _streamId,
                    this,
                    _cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await CancelRemoteAsync();

                Complete(new OperationCanceledException(_cancellationToken));
                throw;
            }

            try
            {
                Current = await _items.Reader.ReadAsync(
                    _cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                await CancelRemoteAsync();

                Complete(new OperationCanceledException(_cancellationToken));
                throw;
            }
            catch (ChannelClosedException)
            {
                if (_items.Reader.Completion.IsFaulted)
                    await _items.Reader.Completion;

                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _dispose(_streamId);
            Complete();

            await Task.CompletedTask;
        }
    }
}