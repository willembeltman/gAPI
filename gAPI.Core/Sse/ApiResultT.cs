namespace gAPI.Core.Sse;

public class ApiResultT<T> : ApiResult
{
    public T? Result { get; set; }
}