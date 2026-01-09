namespace gAPI.EntityFrameworkDisk;

public readonly struct ChangedEntityObject
{
    internal ChangedEntityObject(object itemKey, object cacheItem)
    {
        this.OriginalEntity = itemKey;
        this.ChangedEntity = cacheItem;
    }

    public object OriginalEntity { get; }
    public object ChangedEntity { get; }
}