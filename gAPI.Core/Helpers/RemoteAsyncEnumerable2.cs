using gAPI.Core.Ids;
using System.Threading.Channels;

namespace gAPI.Core.Helpers;

public sealed class RemoteAsyncEnumerable2 : IAsyncEnumerable<byte[]>
{
    private readonly Func<StreamId, Action<byte[]>, Action<Exception?>, CancellationToken, Task> _requestNext;

    public RemoteAsyncEnumerable2(Func<StreamId, Action<byte[]>, Action<Exception?>, CancellationToken, Task> requestNext)
    {
        _requestNext = requestNext;
    }

    public IAsyncEnumerator<byte[]> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Enumerator(_requestNext, cancellationToken);
    }

    private sealed class Enumerator : IAsyncEnumerator<byte[]>
    {
        private readonly StreamId _streamId = StreamId.New();
        private readonly Channel<byte[]> _items = Channel.CreateUnbounded<byte[]>();
        private readonly Func<StreamId, Action<byte[]>, Action<Exception?>, CancellationToken, Task> _requestNext;
        private readonly CancellationToken _cancellationToken;
        private readonly ResettableTimeout _timeout;

        public Enumerator(Func<StreamId, Action<byte[]>, Action<Exception?>, CancellationToken, Task> requestNext, CancellationToken cancellationToken)
        {
            _requestNext = requestNext;
            _cancellationToken = cancellationToken;
            _timeout = new ResettableTimeout(
                TimeSpan.FromSeconds(60),
                () => Complete(new TimeoutException("Remote async enumerable timed out.")));
        }

        public byte[] Current { get; private set; } = default!;

        public void Push(byte[] item)
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
