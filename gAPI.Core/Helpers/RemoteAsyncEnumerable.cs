using gAPI.Core.Ids;
using System.Threading.Channels;

namespace gAPI.Core.Helpers;

public sealed class RemoteAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Func<StreamId, Action<T>, Action<Exception?>, CancellationToken, Task> _requestNext;

    public RemoteAsyncEnumerable(Func<StreamId, Action<T>, Action<Exception?>, CancellationToken, Task> requestNext)
    {
        _requestNext = requestNext;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Enumerator(_requestNext, cancellationToken);
    }

    private sealed class Enumerator : IAsyncEnumerator<T>
    {
        private readonly StreamId _streamId = StreamId.New();
        private readonly Channel<T> _items = Channel.CreateUnbounded<T>();
        private readonly Func<StreamId, Action<T>, Action<Exception?>, CancellationToken, Task> _requestNext;
        private readonly CancellationToken _cancellationToken;
        private readonly ResettableTimeout _timeout;

        public Enumerator(Func<StreamId, Action<T>, Action<Exception?>, CancellationToken, Task> requestNext, CancellationToken cancellationToken)
        {
            _requestNext = requestNext;
            _cancellationToken = cancellationToken;
            _timeout = new ResettableTimeout(
                TimeSpan.FromSeconds(60),
                () => Complete(new TimeoutException("Remote async enumerable timed out.")));
        }

        public T Current { get; private set; } = default!;

        public void Push(T item)
        {
            _items.Writer.TryWrite(item);
            _timeout.Reset();
        }

        public void Complete(Exception? error = null)
        {
            _timeout.Dispose();
            _items.Writer.TryComplete(error);
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                Complete(new OperationCanceledException(_cancellationToken));
                return false;
            }

            await _requestNext(_streamId, Push, Complete, _cancellationToken);

            try
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    Complete(new OperationCanceledException(_cancellationToken));
                    return false;
                }

                Current = await _items.Reader.ReadAsync(_cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                Complete();
                throw;
            }
            catch (ChannelClosedException)
            {
                if (_items.Reader.Completion.IsFaulted)
                    await _items.Reader.Completion;

                return false;
            }
        }

        public ValueTask DisposeAsync()
        {
            Complete();
            return ValueTask.CompletedTask;
        }
    }
}
