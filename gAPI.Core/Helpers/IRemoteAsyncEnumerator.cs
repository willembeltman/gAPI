namespace gAPI.Core.Helpers;

public interface IRemoteAsyncEnumerator<T>
{
    void Complete(Exception? error = null);
    void Push(T item);
}
