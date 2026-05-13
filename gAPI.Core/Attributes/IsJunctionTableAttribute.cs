namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class IsJunctionTableAttribute(Type typeLeft, Type typeRight) : Attribute
{
    public Type TypeLeft { get; } = typeLeft;
    public Type TypeRight { get; } = typeRight;
}