using Microsoft.Extensions.Logging;

namespace gAPI.Core.Client.Config;

public record ClientConfig(
    string ApiBackendUrl,
    string? WssBackendUrl,
    LogLevel MinimumLogLevel = LogLevel.Warning);