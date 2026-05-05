using Microsoft.Extensions.Logging;

namespace gAPI.Core.Wss;

public static class WssLoggerConfig
{
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Error;
}

