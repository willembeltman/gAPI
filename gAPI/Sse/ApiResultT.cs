namespace gAPI.Sse;

public class ApiResultT<T> : ApiResult
{
    public ApiResultT() : base()
    {
    }
    public ApiResultT(T result) : base()
    {
        Result = result;
    }
    public ApiResultT(string? stateData, string? sessionData) : base(stateData, sessionData)
    {
    }
    public ApiResultT(T result, string? stateData, string? sessionData) : base(stateData, sessionData)
    {
        Result = result;
    }

    public T? Result { get; set; }
}