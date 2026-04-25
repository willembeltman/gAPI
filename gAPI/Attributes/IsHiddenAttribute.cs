using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public class IsHiddenAttribute : Attribute
{
}