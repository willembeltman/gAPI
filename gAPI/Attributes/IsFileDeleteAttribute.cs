using System;

namespace gAPI.Attributes
{
    public class IsFileDeleteAttribute : Attribute
    {
        public IsFileDeleteAttribute(Type updateType)
        {
            UpdateType = updateType;
        }

        public Type UpdateType { get; }
    }
}