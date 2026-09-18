using gAPI.Core.Wss;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Core.Wss;

public static class WssLoggerLogDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this WssLoggerLogDto value, WssLoggerLogDto otherValue)
    {
        if (value.Level != otherValue.Level) return true;
        if (value.Message != otherValue.Message) return true;
        if (value.Category != otherValue.Category) return true;
        if (value.Source != otherValue.Source) return true;
        if (value.Timestamp != otherValue.Timestamp) return true;
        if (value.CorrelationId != otherValue.CorrelationId) return true;
        if (value.UserId != otherValue.UserId) return true;
        if (value.Data is null)
        {
            if (otherValue.Data is not null) return true;
        }
        else
        {
            if (otherValue.Data is null) return true;
            if (value.Data.Length != otherValue.Data.Length) return true;
            for (int i1 = 0; i1 < value.Data.Length; i1++)
            {
                var item1 = value.Data[i1];
                var otherItem1 = otherValue.Data[i1];
                if (item1.IsDifferent(otherItem1)) return true;
            }
        }
        if (value.StackTrace != otherValue.StackTrace) return true;
        return false;
    }
}