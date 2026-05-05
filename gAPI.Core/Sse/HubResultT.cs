namespace gAPI.Core.Sse;

public class HubResultT<T> : HubResult
{
    public HubResultT() : base()
    {
    }
    public HubResultT(T result) : this()
    {
        Result = result;
    }

    public T? Result { get; set; }
}