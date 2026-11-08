using gAPI.Core.Dtos;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Interfaces;

public interface IFabricLoggerFactory : ILoggerFactory, ILoggerProvider
{
    Task Send_Log_ToServerAsync(ClientLogDto dto, CancellationToken ct);
    LogLevel MinimumLevel { get; }
}
