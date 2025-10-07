using System;

namespace gAPI.Attributes
{
    public class IsJunctionTableAttribute : Attribute
    {
        public IsJunctionTableAttribute(Type typeLeft, Type typeRight)
        {
            TypeLeft = typeLeft;
            TypeRight = typeRight;
        }

        public Type TypeLeft { get; }
        public Type TypeRight { get; }
    }
}