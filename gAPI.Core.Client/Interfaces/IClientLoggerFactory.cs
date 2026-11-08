using gAPI.Core.Dtos;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Interfaces;

public interface IClientLoggerFactory : ILoggerFactory, ILoggerProvider
{
    Task Send_Log_ToServerAsync(ClientLogDto dto, CancellationToken ct);
    LogLevel MinimalLevel { get; }
}
