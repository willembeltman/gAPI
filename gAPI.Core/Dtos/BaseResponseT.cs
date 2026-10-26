using gAPI.Core.Enums;

namespace gAPI.Core.Dtos;

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

    public T ThrowIfNull()
    {
        if (Response == null || Error.HasValue)
            throw new Exception(Enum.GetName(Error ?? BaseResponseErrorEnum.ErrorNotSpecified));
        return Response;
    }
}