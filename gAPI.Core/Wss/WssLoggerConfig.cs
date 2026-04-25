using Microsoft.Extensions.Logging;

namespace gAPI.Wss;

public static class WssLoggerConfig
{
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Error;
}

