using gAPI.Helpers;

namespace gAPI.AutoSerialiser;

internal static class SerializerFactory<T>
{
    public static SerializerInstance<T> CreateInstance()
    {
        var type = typeof(T);
        var className = $"{type.Name}EntityFactory";
        var readMethodName = "EntityRead";
        var writeMethodName = "EntityWrite";

        var Code = $@"
            using System;
            using System.IO;
            using System.Linq;

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

        return new SerializerInstance<T>(WriteDelegate, ReadDelegate, Code);
    }
    private static string GenerateSerializerCode(Type type, string readMethodName, string writeMethodName)
    {
        var fullClassName = type.FullName;

        var writeCode = string.Empty;
        var readCode = string.Empty;
        var newCode = string.Empty;

        var entityFactoryCollectionType = typeof(SerializerCollection);
        var entityFactoryCollectionTypeFullName = entityFactoryCollectionType.FullName!;
        var entityFactoryCollectionTypeMethod = entityFactoryCollectionType.GetMethods().First().Name;

        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!ReflectionHelper.HasPublicGetter(prop)) continue;
            if (!ReflectionHelper.HasPublicSetter(prop)) continue;
            if (ReflectionHelper.HasNotMappedAttribute(prop)) continue;

            //if (ReflectionHelper.IsOldNavigationEntityProperty(prop))
            //    throw new Exception(
            //        "As of version 1.0.8, we’ve moved from Lazy<T> to ILazy<T> to fix a subtle bug with foreign keys not updating.\n" +
            //        "Sorry for the breaking change — you'll need to update your entity properties.\n" +
            //        "Read more at: https://github.com/willembeltman/EntityFrameworkZip"
            //    );

            if (ReflectionHelper.IsNavigationProperty(prop)) continue;

            var propertyName = prop.Name;
            var propertyType = ReflectionHelper.GetUnderlyingType(prop.PropertyType);

            if (ReflectionHelper.IsPrimitiveTypeOrEnum(propertyType))
            {
                GenerateSerializerCode_SerializePrimative(ref writeCode, ref readCode, prop, propertyName, propertyType);
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
        System.Reflection.PropertyInfo prop, string propertyName, Type propertyType)
    {
        if (ReflectionHelper.IsNulleble(prop))
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
        System.Reflection.PropertyInfo prop, string propertyName, Type propertyType)
    {
        if (ReflectionHelper.IsGenericType(propertyType))
            throw new Exception(
                $"ZipDatabase Exception: Property '{propertyName}' of entity type '{type.FullName}' is not marked as [NotMapped] and is a " +
                $"{propertyType} type, which is not a valid type to serialize. The ZipDatabase engine " +
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

        if (!ReflectionHelper.IsValidChildEntity(propertyType))
            throw new Exception(
                $"ZipDatabase Exception: Child entity type '{propertyType}' of property '{propertyName}' of (child) entity type '{type.FullName}', " +
                $"contains non-primitive types or generic classes that aren't marked as [NotMapped]. The ZipDatabase engine " +
                $"does not support serialisation of non-primitive types or generic classes like Lists or Arrays. The ZipDatabase engine " +
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

        if (ReflectionHelper.IsNulleble(prop))
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
        if (type == typeof(DateTime)) return $"writer.Write(value.{propertyName}.ToBinary());";
        return $"writer.Write(value.{propertyName})";
    }
    private static string GenerateSerializerCode_GetBinaryWriteMethodNull(Type type, string propertyName)
    {
        if (type.IsEnum) return $"writer.Write((int)value.{propertyName}.Value)";
        if (type == typeof(DateTime)) return $"writer.Write(value.{propertyName}.Value.ToBinary());";
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
        if (type == typeof(DateTime)) return "DateTime.FromBinary(reader.ReadInt64())";
        throw new Exception($"Type {type.Name} not supported while its added to the ReflectionHelper.IsPrimitiveType list.");
    }
}