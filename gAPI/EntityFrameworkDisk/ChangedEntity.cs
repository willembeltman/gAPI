namespace gAPI.EntityFrameworkDisk;

public readonly struct ChangedEntity<T>
{
    internal ChangedEntity(object key, T item)
    {
        KeyValue = key;
        Item = item;
    }

    public object KeyValue { get; }
    public T Item { get; }
}