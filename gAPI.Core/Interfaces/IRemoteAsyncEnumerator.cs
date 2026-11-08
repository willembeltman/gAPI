namespace gAPI.Core.Interfaces;

public interface IRemoteAsyncEnumerator<T>
{
    void Complete(Exception? error = null);
    void Push(T item);
}
