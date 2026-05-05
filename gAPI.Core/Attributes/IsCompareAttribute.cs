using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class IsCompareAttribute : CompareAttribute
{
    public IsCompareAttribute(string otherProperty) : base(otherProperty)
    {
    }
}
