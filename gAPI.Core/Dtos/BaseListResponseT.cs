namespace gAPI.Dtos;

public class BaseListResponseT<T> : BaseResponse
{
    public IAsyncEnumerable<T>? Response { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool CanCreate { get; set; }
}