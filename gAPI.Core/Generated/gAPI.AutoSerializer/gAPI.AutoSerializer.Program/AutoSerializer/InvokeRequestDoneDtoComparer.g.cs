using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this InvokeRequestDoneDto value, InvokeRequestDoneDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;
        if (value.StreamIds.Length != otherValue.StreamIds.Length) return true;
        for (int i1 = 0; i1 < value.StreamIds.Length; i1++)
        {
            var item1 = value.StreamIds[i1];
            var otherItem1 = otherValue.StreamIds[i1];
            if (item1 != otherItem1) return true;
        }
        return false;
    }
}