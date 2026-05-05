using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Interfaces;

public interface IWssLoggerFactory : ILoggerFactory
{
    Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct = default);
}
