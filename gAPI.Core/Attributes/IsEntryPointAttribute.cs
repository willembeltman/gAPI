using System;

namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class IsEntryPointAttribute : Attribute
{
}