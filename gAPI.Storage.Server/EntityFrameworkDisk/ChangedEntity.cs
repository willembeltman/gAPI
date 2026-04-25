namespace gAPI.Storage.Server.EntityFrameworkDisk;

public readonly struct ChangedEntity<T>
{
    public ChangedEntity(object key, T item)
    {
        KeyValue = key;
        Item = item;
    }

    public object KeyValue { get; }
    public T Item { get; }
}