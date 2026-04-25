using System;

namespace gAPI.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class IsAuthorizedAttribute : Attribute
{
}
