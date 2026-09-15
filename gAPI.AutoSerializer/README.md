<img src="../gAPI_logo.png" alt="gAPI by Willem-Jan Beltman - Logo" width="300">

# gAPI.AutoSerializer

## Compile-time serializers for .NET types using a Roslyn source generator.

gAPI.AutoSerializer generates strongly typed serialization, copying and comparison code directly from your C# types. No reflection is required at runtime.

The serializer is designed around explicit C# types and their structure. When a type contains another supported type, the generated serializer automatically uses the serializer for that child type.

### FEATURES

- Roslyn source generation
- No runtime reflection
- Binary serialization using BinaryWriter / BinaryReader
- High-performance Span<byte> serialization
- MultipartFormDataContent serialization
- Deep CreateCopy()
- Deep IsDifferent()
- Nested objects are automatically supported
- List<T> and arrays are supported
- Classes are supported
- Records are supported when they follow the normal immutable-record pattern
- Custom serializers can be added for special types
- Schema and type identifiers are embedded in binary serialization

## GETTING STARTED

Add the GenerateSerializer attribute to a class or record:

``` csharp
[GenerateSerializer]
public class AuthStateDto
{
	public AuthStateUserDto? User { get; set; }
	public bool ForceReconnect { get; set; }
}
```

The source generator generates the required serialization and helper code at compile time.

You do not need to manually implement a serializer for AuthStateDto.

## GENERATED FUNCTIONALITY

For a type marked with [GenerateSerializer], the generator can produce several implementations.

### BINARYWRITER / BINARYREADER

Extension methods for BinaryWriter and BinaryReader are generated.

For example:

``` csharp
writer.Write(value);

var value = reader.ReadAuthStateDto();
```

The binary format contains:

- A gAPI magic value
- A type identifier
- A schema hash
- The serialized object data

The generated serializer contains constants such as:

``` csharp
public const ushort Magic = (ushort)0x4741;
public const uint TypeId = 0x2735AB25;
public const uint SchemaHash = 0x2F36FFA4;
```

When reading, the magic value, type ID and schema hash are validated.

This allows incompatible or incorrectly typed binary data to be detected instead of silently deserializing it.

### SPAN<byte>

A Span<byte> based serializer is generated as well.

It can write directly into a supplied buffer:

``` csharp
AuthStateDtoSpanSerializer.Write(
	ref span,
	ref offset,
	value);
```

Reading is performed directly from a ReadOnlySpan<byte>:

``` csharp
var value = AuthStateDtoSpanSerializer.ReadAuthStateDto(
	span,
	ref offset);
```

A Length method is also generated so the required buffer size can be determined before serialization:

``` csharp
var offset = 0;
var length = AuthStateDtoSpanSerializer.Length(ref offset, value);
```

The span serializer operates directly on the supplied buffer and maintains the current position through the offset parameter.

### MULTIPARTFORMDATACONTENT

A serializer for MultipartFormDataContent is generated:

``` csharp
AuthStateDtoMultipartFormDataContentSerializer.Write(
	content,
	"value",
	value);
```

Properties are added using their property names.

Nested generated types are serialized recursively.

### CREATECOPY

A deep-copy extension method is generated:

``` csharp
var copy = value.CreateCopy();
```

For example:

``` csharp
public static AuthStateDto CreateCopy(this AuthStateDto value)
{
	var copy = new AuthStateDto();
	copy.User = value.User == null ? null : value.User.CreateCopy();
	copy.ForceReconnect = value.ForceReconnect;
	return copy;
}
```

Nested objects are copied recursively instead of simply copying their references.

### ISDIFFERENT

A structural comparison extension method is generated:

``` csharp
if (value.IsDifferent(otherValue))
{
	// Values differ
}
```

Nested generated types are compared recursively.

For example, if AuthStateDto contains an AuthStateUserDto, the generated comparison calls User.IsDifferent(otherValue.User).

This provides a deep structural comparison without requiring a general-purpose reflection-based object comparer.

### NESTED TYPES

Child objects are automatically included.

For example:

``` csharp
[GenerateSerializer]
public class AuthStateUserDto
{
	public string Name { get; set; } = "";
}

[GenerateSerializer]
public class AuthStateDto
{
	public AuthStateUserDto? User { get; set; }
	public bool ForceReconnect { get; set; }
}
```

When AuthStateDto is generated, its generated serializer automatically uses the generated serializer for AuthStateUserDto.

This works recursively for supported nested types.

### LISTS AND ARRAYS

List<T> and arrays are supported.

For example:

``` csharp
[GenerateSerializer]
public class ProjectDto
{
	public string Name { get; set; } = "";
	public List<string> Tags { get; set; } = [];
	public AuthStateUserDto[] Users { get; set; } = [];
}
```

The generator handles the collection serialization and recursively serializes supported child types.

### RECORDS

Records are supported, provided they follow the normal immutable record model.

For example:

``` csharp
[GenerateSerializer]
public record UserDto(
	int Id,
	string Name,
	string Email);
```

The important requirement is that the record can be reconstructed through its constructor.

Records work best when they are used as records are intended to be used:

- Immutable
- Values supplied through the constructor
- No hidden mutable state
- Properties correspond to constructor parameters

For example:

``` csharp
[GenerateSerializer]
public record UserDto(
	int Id,
	string Name);
```

gAPI.AutoSerializer does not attempt to support every possible C# construct that can be created with records.

This is intentional.

### CLASSES

Normal mutable classes are supported as well:

``` csharp
[GenerateSerializer]
public class UserDto
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}
```

The generated deserializer creates the object and assigns its properties.

### SUPPORTED TYPES

The serializer currently supports primitive and framework types implemented by the built-in serializers, together with:

- Generated classes
- Generated records
- Nullable values
- Nested objects
- List<T>
- T[]

Support is determined by the available serializer extensions.

Specialized types can provide their own serializer implementation.


# CUSTOM SERIALIZERS AND EXTENSIONS

gAPI.AutoSerializer is extensible. If a type is not handled by the built-in serializers, you can provide your own serialization methods.

The generator recognizes serializer implementations through attributes on static extension methods.

A custom implementation can provide one or more of the following operations:


## BinaryWriter / BinaryReader

The BinaryWriter and BinaryReader serializer methods use:

### [IsSerializerWrite]

Defines the BinaryWriter serialization method.

Expected signature:

``` csharp

public static void Write(
	this BinaryWriter writer,
	T value)

```

and:

### [IsSerializerRead]

Defines the BinaryReader deserialization method.

Expected signature:

``` csharp

public static T ReadT(
	this BinaryReader reader)

```

Example:

``` csharp

[IsSerializerWrite]
public static void Write(
	this BinaryWriter writer,
	MySpecialType value)
{
	// Write value
}

[IsSerializerRead]
public static MySpecialType ReadMySpecialType(
	this BinaryReader reader)
{
	// Read value
}

```

## Span<byte>

For high-performance span serialization, provide:

### [IsSpanSerializerWrite]

Defines the Span<byte> serialization method.

Expected signature:

``` csharp

public static void Write(
	this ref Span<byte> span,
	ref int offset,
	T value)

```

### [IsSpanSerializerRead]

Defines the Span<byte> deserialization method.

Expected signature:

``` csharp

public static T ReadT(
	this ReadOnlySpan<byte> span,
	ref int offset)

```

### [IsSpanSerializerLength]

Defines the method used to calculate the serialized size for Span<byte> serialization.

Expected signature:

``` sharp

public static int Length(
	ref int offset,
	T value)

```

Example:

``` sharp

[IsSpanSerializerWrite]
public static void Write(
	this ref Span<byte> span,
	ref int offset,
	MySpecialType value)
{
	// Write value into span
}

[IsSpanSerializerRead]
public static MySpecialType ReadMySpecialType(
	this ReadOnlySpan<byte> span,
	ref int offset)
{
	// Read value from span
}

[IsSpanSerializerLength]
public static int Length(
	ref int offset,
	MySpecialType value)
{
	// Add required length to offset
	return offset;
}

```

The Length method allows the required buffer size to be calculated before writing.

## MultipartFormDataContent

A custom MultipartFormDataContent serializer uses:

### [IsMultipartFormDataContentSerializer]

Defines the MultipartFormDataContent serialization method.

Expected signature:

``` csharp

public static void Write(
	this MultipartFormDataContent content,
	string name,
	T value)

```

Example:

``` csharp

[IsMultipartFormDataContentSerializer]
public static void Write(
	this MultipartFormDataContent content,
	string name,
MySpecialType value)
{
	// Add value to multipart content
}

```

## Comparers

If the type requires custom comparison logic, provide:

### [IsComparer]

Defines the structural comparison method.

Expected signature:

``` csharp

public static bool IsDifferent(
	this T value,
	T otherValue)

```

Example:

``` csharp

[IsComparer]
public static bool IsDifferent(
	this MySpecialType value,
	MySpecialType otherValue)
{
	return value.SomeValue != otherValue.SomeValue;
}

```

The generated serializers can then use:

``` csharp

value.IsDifferent(otherValue);

```

## CreateCopy

A custom deep-copy implementation uses:

### [IsCreateCopy]

Defines the deep-copy method.

Expected signature:

``` csharp

public static T CreateCopy(
	this T value)

```

Example:

``` csharp

[IsCreateCopy]
public static MySpecialType CreateCopy(
	this MySpecialType value)
{
	return new MySpecialType(value.SomeValue);
}

```


### COMPOSING WITH GENERATED TYPES

Custom serializers can be combined with generated serializers.

For example, if a generated DTO contains a custom type:

``` csharp

[GenerateSerializer]
public class MyDto
{
	public MySpecialType Value { get; set; }
}

```

the generated MyDto serializer will use the custom serializer implementation for MySpecialType.

This means special types can be integrated into the generated serialization graph without modifying gAPI.AutoSerializer itself.

The same mechanism is used internally by the built-in serializers.

## LICENSE

See the repository for license information.