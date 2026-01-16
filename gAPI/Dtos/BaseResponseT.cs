namespace gAPI.Dtos;

public class BaseResponseT<T> : BaseResponse
{
    public T? Response { get; set; }
}