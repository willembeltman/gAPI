using System.Diagnostics;

namespace gAPI.Core.Server.Interfaces;

public interface ISpeedGetter
{
    Stopwatch Stopwatch { get; }

    (int count, long bytes) GetReceiveBytesPerSecond();
    (int count, long bytes) GetSendBytesPerSecond();
}