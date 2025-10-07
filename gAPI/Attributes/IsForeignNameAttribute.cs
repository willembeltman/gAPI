using System;

namespace gAPI.Attributes
{
    public class IsForeignNameAttribute : Attribute
    {
        public IsForeignNameAttribute(string foreignKeyName)
        {
            ForeignKeyName = foreignKeyName;
        }

        public string ForeignKeyName { get; }
    }
}