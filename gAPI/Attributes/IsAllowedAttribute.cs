using System;

namespace gAPI.Attributes
{
    public class IsAllowedAttribute : Attribute
    {
        public IsAllowedAttribute(Type allowedType)
        {
            AllowedType = allowedType;
        }

        public Type AllowedType { get; set; }
    }
}