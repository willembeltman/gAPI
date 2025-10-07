using System;

namespace gAPI.Attributes
{
    public class ApiNameAttribute : Attribute
    {
        public ApiNameAttribute()
        {
        }
        public ApiNameAttribute(string name)
        {
            Name = name;
        }
        public string? Name { get; }
    }
}
