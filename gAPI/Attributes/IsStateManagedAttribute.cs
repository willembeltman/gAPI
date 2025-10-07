using System;

namespace gAPI.Attributes
{
    public class IsStateManagedAttribute : Attribute
    {
        public IsStateManagedAttribute(string userProperty)
        {
            UserProperty = userProperty;
        }

        public string UserProperty { get; }
    }
}