using System;

namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public class IsHiddenAttribute : Attribute
{
}