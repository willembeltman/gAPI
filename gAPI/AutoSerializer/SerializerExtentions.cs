using System.IO;

namespace gAPI.AutoSerialiser;

public static class SerializerExtentions
{
    public static void Read<T>(this BinaryReader br)
        where T : class
    {
        var serializer = SerializerCollection.GetOrCreate<T>();
        serializer.Read(br);
    }
    public static void Write<T>(this BinaryWriter bw, T entity)
        where T : class
    {
        var serializer = SerializerCollection.GetOrCreate<T>();
        serializer.Write(bw, entity);
    }
}
