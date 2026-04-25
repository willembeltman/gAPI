namespace gAPI.Storage.Server.EntityFrameworkDisk;

public readonly struct ChangedEntityObject
{
    public ChangedEntityObject(object itemKey, object cacheItem)
    {
        this.OriginalEntity = itemKey;
        this.ChangedEntity = cacheItem;
    }

    public object OriginalEntity { get; }
    public object ChangedEntity { get; }
}