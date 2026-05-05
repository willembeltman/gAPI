using gAPI.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Wss;

[GenerateSerializer]
public class WssLoggerLogDto
{
    public LogLevel Level { get; set; }

    public string Message { get; set; } = default!;

    // Optional but extremely useful
    public string? Category { get; set; }        // "Auth", "API", "SignalR", "UI"
    public string? Source { get; set; }          // "React", "Vue", "Blazor", etc.

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Correlation
    public string? CorrelationId { get; set; }   // tie FE + BE logs together
    public string? UserId { get; set; }

    // Extra structured data
    public SignalRLogDataDto[]? Data { get; set; }

    // For errors
    public string? StackTrace { get; set; }
}
public class SignalRLogDataDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}

