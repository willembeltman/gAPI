using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

public static class SendRequestDoneClientDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this SendRequestDoneClientDto value, SendRequestDoneClientDto otherValue)
    {
        if (value.Routing != otherValue.Routing) return true;

        if (value.Cancelled != otherValue.Cancelled) return true;
        if (value.ExceptionMessage != otherValue.ExceptionMessage) return true;
        if (value.StateIsChanged != otherValue.StateIsChanged) return true;
        if (value.StateData != otherValue.StateData) return true;
        return false;
    }
}