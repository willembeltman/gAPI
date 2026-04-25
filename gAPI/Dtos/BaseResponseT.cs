namespace gAPI.Dtos;

public class BaseResponseT<T> : BaseResponse
{
    public BaseResponseT()
    {
    }

    public BaseResponseT(T response)
    {
        Response = response;
    }

    public T? Response { get; set; }
}