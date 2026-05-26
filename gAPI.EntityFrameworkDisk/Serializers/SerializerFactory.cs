using gAPI.Core.Ids;
using gAPI.EntityFrameworkDisk.Helpers;
using gAPI.EntityFrameworkDisk.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace gAPI.EntityFrameworkDisk.Serializers;

internal static class SerializerFactory<T>
{
    public static SerializerInstance<T> CreateInstance()
    {
        var type = typeof(T);
        var className = $"{type.Name}EntityFactory";
        var readMethodName = "EntityRead";
        var writeMethodName = "EntityWrite";

        var Code = $@"
            using gAPI.EntityFrameworkDisk.Serializers.Special;
            using System;
            using System.IO;
            using System.Linq;
            using gAPI.Core.Ids;

            public static class {className}
            {{
                {GenerateSerializerCode(type, readMethodName, writeMethodName)}
            }}";

        var asm = CodeCompiler.Compile(Code);
        var serializerType = asm.GetType(className);

        var readMethod = serializerType!.GetMethod(readMethodName)!;
        var writeMethod = serializerType.GetMethod(writeMethodName)!;

        var ReadDelegate = (Func<BinaryReader, T>)Delegate.CreateDelegate(
            typeof(Func<BinaryReader, T>), readMethod);

        var WriteDelegate = (Action<BinaryWriter, T>)Delegate.CreateDelegate(
             typeof(Action<BinaryWriter, T>), writeMethod);

        var instance = new SerializerInstance<T>(WriteDelegate, ReadDelegate, Code);

        Console.WriteLine($"{DateTime.Now:t} Generated '{className}'");

        return instance;
    }
    private static string GenerateSerializerCode(Type type, string readMethodName, string writeMethodName)
    {
        var fullClassName = type.FullName;

        var writeCode = string.Empty;
        var readCode = string.Empty;
        var newCode = string.Empty;

        var entityFactoryCollectionType = typeof(Serializer);
        var entityFactoryCollectionTypeFullName = entityFactoryCollectionType.FullName!;
        var entityFactoryCollectionTypeMethod = entityFactoryCollectionType.GetMethods().First().Name;
        var isRecord = IsRecord(type);

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!HasPublicGetter(prop)) continue;
            if (!HasPublicSetter(prop)) continue;

            if (HasNotMappedAttribute(prop)) continue;
            if (IsNavigationProperty(prop)) continue;

            var propertyName = prop.Name;
            var propertyType = GetUnderlyingType(prop.PropertyType);

            if (IsPrimitiveTypeOrEnum(propertyType))
            {
                GenerateSerializerCode_SerializePrimative(ref writeCode, ref readCode, prop, propertyName, propertyType);
            }
            else if (isRecord)
            {
                continue;
            }
            else
            {
                GenerateSerializerCode_SerializeObject(type, ref writeCode, ref readCode, entityFactoryCollectionTypeFullName, entityFactoryCollectionTypeMethod, prop, propertyName, propertyType);
            }

            newCode += $@"
                {propertyName} = {propertyName}1,";
        }

        return $@"
                public static void {writeMethodName}(BinaryWriter writer, {fullClassName} value)
                {{
                    {writeCode}
                }}

                public static {fullClassName} {readMethodName}(BinaryReader reader)
                {{
                    {readCode}

                    var item = new {fullClassName}
                    {{{newCode}
                    }};

                    return item;
                }}
                ";
    }
    private static void GenerateSerializerCode_SerializePrimative(
        ref string writeCode, ref string readCode,
        PropertyInfo prop, string propertyName, Type propertyType)
    {
        if (IsNulleble(prop))
        {
            var readMethod = GenerateSerializerCode_GetBinaryReadMethod(propertyType);
            var writeMethod = GenerateSerializerCode_GetBinaryWriteMethodNull(propertyType, propertyName);

            writeCode += $@"
                        if (value.{propertyName} == null)
                            writer.Write(true);
                        else
                        {{
                            writer.Write(false);
                            {writeMethod};
                        }}";

            readCode += $@"
                        {propertyType.FullName}? {propertyName}1 = null;
                        if (!reader.ReadBoolean())
                        {{
                            {propertyName}1 = {readMethod};
                        }}";

        }
        else
        {
            var readMethod = GenerateSerializerCode_GetBinaryReadMethod(propertyType);
            var writeMethod = GenerateSerializerCode_GetBinaryWriteMethodNotNull(propertyType, propertyName);

            writeCode += $@"
                        {writeMethod};";

            readCode += $@"
                        var {propertyName}1 = {readMethod};";
        }
    }
    private static void GenerateSerializerCode_SerializeObject(
        Type type, ref string writeCode, ref string readCode,
        string serializerCollectionTypeFullName, string serializerCollectionTypeMethod,
        PropertyInfo prop, string propertyName, Type propertyType)
    {
        if (IsGenericType(propertyType))
            throw new Exception(
                $"AutoSerializer Exception: Property '{propertyName}' of entity type '{type.FullName}' is not marked as [NotMapped] and is a " +
                $"{propertyType} type, which is not a valid type to serialize. The AutoSerializer engine " +
                $"does not support generic types like Lists and Array, it only support:\r\n" +
                $"\r\n" +
                $"- Primative types.\r\n" +
                $"- DateTime's.\r\n" +
                $"- Enum's.\r\n" +
                $"- Navigation properties with 'virtual ILazy<>' (with optional ForeignKeyAttribute).\r\n" +
                $"- Navigation collections with 'virtual ICollection<>' or 'virtual IEnumerable<>' (with optional ForeignKeyAttribute).\r\n" +
                $"- And non-generic struct's and classes(which in turn support the same kind of properties). \r\n" +
                $"\r\n" +
                $"You can ofcourse use generic types like Lists or Arrays but only marked as [NotMapped] " +
                $"to signal those properties are not serialized when the dbcontext is saved. Please mark " +
                $"those properties as [NotMapped] if this is intended.\r\n" +
                $"\r\n" +
                $"Or maybe you forgot `virtual` infront of the navigation properties?");

        if (!IsValidChildEntity(propertyType))
            throw new Exception(
                $"AutoSerializer Exception: Child entity type '{propertyType}' of property '{propertyName}' of (child) entity type '{type.FullName}', " +
                $"contains non-primitive types or generic classes that aren't marked as [NotMapped]. The AutoSerializer engine " +
                $"does not support serialisation of non-primitive types or generic classes like Lists or Arrays. The AutoSerializer engine " +
                $"only supports child entity types containing:\r\n" +
                $"\r\n" +
                $"- Primative types.\r\n" +
                $"- DateTime's.\r\n" +
                $"- Enum's.\r\n" +
                $"- Navigation properties with 'virtual ILazy<>' (with optional ForeignKeyAttribute).\r\n" +
                $"- Navigation collections with 'virtual ICollection<>' or 'virtual IEnumerable<>' (with optional ForeignKeyAttribute).\r\n" +
                $"- And non-generic struct's and classes(which in turn support the same kind of properties). \r\n" +
                $"\r\n" +
                $"You can ofcourse use generic types like Lists or Arrays but only marked as [NotMapped] " +
                $"to signal those properties are not serialized when the dbcontext is saved. Please mark " +
                $"those properties as [NotMapped] if this is intended.");

        if (IsNulleble(prop))
        {
            writeCode += $@"

                        if (value.{propertyName} == null)
                            writer.Write(true);
                        else
                        {{
                            writer.Write(false);
                            var {propertyName}Serializer = {serializerCollectionTypeFullName}.{serializerCollectionTypeMethod}<{propertyType.FullName}>();
                            {propertyName}Serializer.Write(writer, value.{propertyName});
                        }}";

            readCode += $@"

                        {propertyType}? {propertyName}1 = null;
                        if (!reader.ReadBoolean())
                        {{
                            var {propertyName}Serializer = {serializerCollectionTypeFullName}.{serializerCollectionTypeMethod}<{propertyType.FullName}>();
                            {propertyName}1 = {propertyName}Serializer.Read(reader);
                        }}";
        }
        else
        {
            writeCode += $@"

                        var {propertyName}Serializer = {serializerCollectionTypeFullName}.{serializerCollectionTypeMethod}<{propertyType.FullName}>();
                        {propertyName}Serializer.Write(writer, value.{propertyName});";

            readCode += $@"

                        var {propertyName}Serializer = {serializerCollectionTypeFullName}.{serializerCollectionTypeMethod}<{propertyType.FullName}>();
                        var {propertyName}1 = {propertyName}Serializer.Read(reader);";
        }
    }
    private static string GenerateSerializerCode_GetBinaryWriteMethodNotNull(Type type, string propertyName)
    {
        if (type.IsEnum) return $"writer.Write((int)value.{propertyName})";
        //if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return $"writer.Write(value.{propertyName}.ToBinary());";
        return $"writer.Write(value.{propertyName})";
    }
    private static string GenerateSerializerCode_GetBinaryWriteMethodNull(Type type, string propertyName)
    {
        if (type.IsEnum) return $"writer.Write((int)value.{propertyName}.Value)";
        //if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return $"writer.Write(value.{propertyName}.Value.ToBinary());";
        if (type == typeof(string)) return $"writer.Write(value.{propertyName})";
        return $"writer.Write(value.{propertyName}.Value)";
    }
    private static string GenerateSerializerCode_GetBinaryReadMethod(Type type)
    {
        if (type.IsEnum) return $"({type.FullName})reader.ReadInt32()";
        if (type == typeof(bool)) return "reader.ReadBoolean()";
        if (type == typeof(byte)) return "reader.ReadByte()";
        if (type == typeof(sbyte)) return "reader.ReadSByte()";
        if (type == typeof(char)) return "reader.ReadChar()";
        if (type == typeof(decimal)) return "reader.ReadDecimal()";
        if (type == typeof(double)) return "reader.ReadDouble()";
        if (type == typeof(float)) return "reader.ReadSingle()";
        if (type == typeof(short)) return "reader.ReadInt16()";
        if (type == typeof(ushort)) return "reader.ReadUInt16()";
        if (type == typeof(int)) return "reader.ReadInt32()";
        if (type == typeof(uint)) return "reader.ReadUInt32()";
        if (type == typeof(long)) return "reader.ReadInt64()";
        if (type == typeof(ulong)) return "reader.ReadUInt64()";
        if (type == typeof(string)) return "reader.ReadString()";
        if (type == typeof(DateTime)) return "reader.ReadDateTime()";
        if (type == typeof(DateTimeOffset)) return "reader.ReadDateTimeOffset()";

        if (type == typeof(ConnectionId)) return "reader.ReadConnectionId()";
        if (type == typeof(FabricHostId)) return "reader.ReadFabricHostId()";
        if (type == typeof(RequestId)) return "reader.ReadRequestId()";
        if (type == typeof(SessionId)) return "reader.ReadSessionId()";
        if (type == typeof(SseHostId)) return "reader.ReadSseHostId()";
        if (type == typeof(SseManagerId)) return "reader.ReadSseManagerId()";
        if (type == typeof(ServiceId)) return "reader.ReadSseServiceId()";
        if (type == typeof(ServiceMethodId)) return "reader.ReadServiceMethodId()";
        if (type == typeof(UserId)) return "reader.ReadUserId()";

        //if (type == typeof(DateTime)) return "DateTime.FromBinary(reader.ReadInt64())";
        //if (type == typeof(DateTimeOffset)) return @$"DateTimeOffset.FromBinary(reader.ReadInt64())";
        throw new Exception($"Type {type.Name} not supported while its added to the IsPrimitiveType list.");
    }

    public static bool IsRecord(Type type)
    {
        return type.GetMethods()
            .Any(m => m.Name == "<Clone>$");
    }

    public static bool IsNavigationProperty(PropertyInfo prop)
    {
        return IsNavigationCollectionProperty(prop) || IsNavigationEntityProperty(prop);
    }
    public static bool IsICollection(PropertyInfo prop) => IsICollection(prop.PropertyType);
    public static bool IsICollection(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(ICollection<>);
    }
    public static bool IsIEnumerable(PropertyInfo prop) => IsIEnumerable(prop.PropertyType);
    public static bool IsIEnumerable(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
    }
    public static bool IsILazy(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(ILazy<>);
    }
    public static bool IsVirtual(PropertyInfo prop)
    {
        var method = prop.GetGetMethod(true);
        if (method == null)
            return false;
        return method.IsVirtual && !method.IsFinal;
    }
    public static bool HasAnyProperties(Type propType)
    {
        return propType.GetProperties().Length != 0;
    }
    public static bool HasPublicGetter(PropertyInfo prop)
    {
        var getter = prop.GetGetMethod(false);
        return getter != null;
    }
    public static bool HasPublicSetter(PropertyInfo prop)
    {
        return prop.GetSetMethod(false) != null;
    }
    public static bool HasNotMappedAttribute(PropertyInfo prop)
    {
        return
            prop.GetCustomAttribute<NotMappedAttribute>() != null;
    }
    public static bool IsNulleble(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }
        else
        {
            return true; // Referentietypen zijn altijd nullable
        }
    }
    public static bool IsNavigationEntityProperty(PropertyInfo prop)
    {
        return
            IsVirtual(prop) &&
            IsILazy(prop);
    }
    public static bool IsNavigationCollectionProperty(PropertyInfo prop)
    {
        return
            IsVirtual(prop) &&
            (IsIEnumerable(prop) || IsICollection(prop));
    }
    public static bool IsValidChildEntity(Type type, HashSet<Type>? visitedTypes = null)
    {
        visitedTypes ??= [];
        if (visitedTypes.Contains(type))
            return true; // Avoid cycles
        visitedTypes.Add(type);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!HasPublicGetter(prop)) continue;
            if (!HasPublicSetter(prop)) continue;

            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (HasNotMappedAttribute(prop)) continue;
            if (IsNavigationEntityProperty(prop)) continue;
            if (IsNavigationCollectionProperty(prop)) continue;

            if (IsPrimitiveTypeOrEnum(propType)) continue;

            if (IsGenericType(propType)) return false;
            if (!HasAnyProperties(propType)) return false;

            if (propType.IsClass || propType.IsValueType)
            {
                if (!IsValidChildEntity(propType, visitedTypes))
                    return false;
            }
            else
            {
                return false;
            }

            return true;
        }

        return true;
    }
    public static bool IsGenericType(Type propType)
    {
        return propType.IsGenericType;
    }
    public static bool IsPrimitiveTypeOrEnum(Type propertyType)
    {
        return PrimitiveTypes.Contains(propertyType) || propertyType.IsEnum;
    }
    private static readonly HashSet<Type> PrimitiveTypes =
    [
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        typeof(decimal),
        typeof(double),
        typeof(float),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(ConnectionId),
        typeof(FabricHostId),
        typeof(RequestId),
        typeof(SessionId),
        typeof(SseHostId),
        typeof(SseManagerId),
        typeof(ServiceId),
        typeof(ServiceMethodId),
        typeof(UserId)
    ];

    public static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}