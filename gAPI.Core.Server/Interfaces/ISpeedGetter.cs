namespace gAPI.Core.Server.Interfaces;

public interface ISpeedGetter
{
    (int count, long bytes) GetReceiveBytesPerSecond();
    (int count, long bytes) GetSendBytesPerSecond();
}