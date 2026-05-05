using System;
using System.Collections.Generic;
using System.Text;

namespace gAPI.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class GenerateSerializerAttribute : Attribute
{
}
