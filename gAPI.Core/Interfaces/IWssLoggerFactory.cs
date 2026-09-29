using gAPI.Core.Wss;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Interfaces;

public interface IClientLoggerFactory : ILoggerFactory, ILoggerProvider
{
    Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct);
    LogLevel MinimalLevel { get; }
}

public interface IFabricLoggerFactory : ILoggerFactory, ILoggerProvider
{
    Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct);
    LogLevel MinimumLevel { get; }
}
