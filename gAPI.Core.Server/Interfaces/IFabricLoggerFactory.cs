using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Interfaces;

public interface IFabricLoggerFactory : ILoggerFactory, ILoggerProvider
{
    Task Send_Log_ToServerAsync(ClientLogDto dto, CancellationToken ct);
    LogLevel MinimumLevel { get; }
}
