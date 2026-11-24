using gAPI.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public class LogDto
{
    public LogLevel Level { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Category { get; set; }
    public string? Source { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    // Event metadata
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public string? MessageTemplate { get; set; }

    // Structured logging
    public Dictionary<string, string?> Properties { get; set; } = new();
    public string[] Scopes { get; set; } = [];

    // Correlation
    public string? CorrelationId { get; set; }

    // Exceptions
    public string? ExceptionDetails { get; set; }
}