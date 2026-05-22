using gAPI.Core.Ids;

namespace gAPI.EntityFrameworkDisk.Serializers.Special;

// Dit zijn overloaders voor de auto serializer, niet verwijderen aub. Deze moeten ook niet door de autoserializer gebruikt worden
public static class SerializerExtensions
{
    //public static T Read<T>(this BinaryReader br)
    //    where T : class
    //{
    //    var serializer = Serializer.GetOrCreate<T>();
    //    return serializer.Read(br);
    //}
    //public static void Write<T>(this BinaryWriter bw, T entity)
    //    where T : class
    //{
    //    var serializer = Serializer.GetOrCreate<T>();
    //    serializer.Write(bw, entity);
    //}

    public static DateTime ReadDateTime(this BinaryReader br)
    {
        return DateTime.FromBinary(br.ReadInt64());
    }
    public static void Write(this BinaryWriter bw, DateTime value)
    {
        bw.Write(value.ToBinary());
    }

    public static DateTimeOffset ReadDateTimeOffset(this BinaryReader br)
    {
        long ticks = br.ReadInt64();
        int offsetMinutes = br.ReadInt32();
        return new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
    }
    public static void Write(this BinaryWriter bw, DateTimeOffset value)
    {
        bw.Write(value.Ticks);              // long
        bw.Write((int)value.Offset.TotalMinutes); // int
    }

    public static ConnectionId ReadConnectionId(this BinaryReader br)
    {
        return new ConnectionId(br.ReadInt64());
    }
    public static void Write(this BinaryWriter bw, ConnectionId value)
    {
        bw.Write(value.Value);
    }

    public static UserId ReadUserId(this BinaryReader br)
    {
        if (br.ReadBoolean())
        {
            return new UserId(null);
        }
        return new UserId(br.ReadString());
    }
    public static void Write(this BinaryWriter bw, UserId value)
    {
        if (value.Value == null)
        {
            bw.Write(true);
            return;
        }
        bw.Write(false);
        bw.Write(value.Value);
    }

    public static ServiceMethodId ReadServiceMethodId(this BinaryReader br)
    {
        return new ServiceMethodId(br.ReadString());
    }
    public static void Write(this BinaryWriter bw, ServiceMethodId value)
    {
        bw.Write(value.Value);
    }

    public static ServiceId ReadServiceId(this BinaryReader br)
    {
        return new ServiceId(br.ReadString());
    }
    public static void Write(this BinaryWriter bw, ServiceId value)
    {
        bw.Write(value.Value);
    }

    public static FabricHostId ReadFabricHostId(this BinaryReader br)
    {
        return new FabricHostId(br.ReadInt64());
    }
    public static void Write(this BinaryWriter bw, FabricHostId value)
    {
        bw.Write(value.Value);
    }

    public static RequestId ReadRequestId(this BinaryReader br)
    {
        return new RequestId(br.ReadString());
    }
    public static void Write(this BinaryWriter bw, RequestId value)
    {
        bw.Write(value.Value);
    }

    public static SessionId ReadSessionId(this BinaryReader br)
    {
        return new SessionId(br.ReadString());
    }
    public static void Write(this BinaryWriter bw, SessionId value)
    {
        bw.Write(value.Value);
    }

    public static SseHostId ReadSseHostId(this BinaryReader br)
    {
        return new SseHostId(br.ReadInt64());
    }
    public static void Write(this BinaryWriter bw, SseHostId value)
    {
        bw.Write(value.Value);
    }

    public static SseManagerId ReadSseManagerId(this BinaryReader br)
    {
        return new SseManagerId(br.ReadInt64());
    }
    public static void Write(this BinaryWriter bw, SseManagerId value)
    {
        bw.Write(value.Value);
    }
}
