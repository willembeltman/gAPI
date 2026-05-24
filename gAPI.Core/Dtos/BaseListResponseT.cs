namespace gAPI.Core.Dtos;

public class BaseListResponseT<T> : BaseResponse
{
    public T[]? Response { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool CanCreate { get; set; }
}