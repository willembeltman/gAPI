namespace gAPI.Storage.Server.AutoSerializer;

public static class Serializer
{
    public static readonly Dictionary<Type, object> Serializers = new Dictionary<Type, object>();

    public static SerializerInstance<T> GetOrCreate<T>()
    {
        var entityType = typeof(T);
        if (Serializers.TryGetValue(entityType, out var serializer))
        {
            return (SerializerInstance<T>)serializer;
        }
        else
        {
            var newSerializer = SerializerFactory<T>.CreateInstance();
            Serializers[entityType] = newSerializer;
            return newSerializer;
        }
    }
}