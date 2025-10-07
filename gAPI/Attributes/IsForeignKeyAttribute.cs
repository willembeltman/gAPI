using System;

namespace gAPI.Attributes
{
    public class IsForeignKeyAttribute : Attribute
    {
        public IsForeignKeyAttribute(Type type)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type), "Type cannot be null.");
        }

        public Type Type { get; }
    }
}
