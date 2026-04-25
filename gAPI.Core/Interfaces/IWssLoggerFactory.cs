using gAPI.Wss;
using Microsoft.Extensions.Logging;

namespace gAPI.Interfaces;

public interface IWssLoggerFactory : ILoggerFactory
{
    Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct = default);
}
