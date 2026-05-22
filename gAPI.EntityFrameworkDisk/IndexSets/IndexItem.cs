namespace gAPI.EntityFrameworkDisk.IndexSets;

public class IndexItem
{
    public IndexItem(object? key, long indexPosition, long dataPosition)
    {
        Key = key;
        IndexPosition = indexPosition;
        DataPosition = dataPosition;
    }

    public object? Key { get; }
    public long IndexPosition { get; }
    public long DataPosition { get; }
}
