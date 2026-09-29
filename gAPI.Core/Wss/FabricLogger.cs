using gAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Wss;

public sealed class FabricLogger(
    string category,
    IFabricLoggerFactory hub) 
    : ILogger
{
    public string Category { get; } = category;

    public bool IsEnabled(LogLevel level)
        => level >= hub.MinimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter?.Invoke(state, exception) ?? state?.ToString() ?? string.Empty;

        LogToHub(logLevel, message, exception);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Slimme manier: als state een dictionary is, sla hem door, anders noop
        if (state is IDictionary<string, object?> dict)
            return new Scope(dict);

        return null;
    }

    // Eigen methode die naar SignalR stuurt
    public void LogToHub(
        LogLevel level,
        string message,
        Exception? exception = null)
    {
        if (!IsEnabled(level))
            return;

        _ = Task.Run(async () =>
        {
            var dto = new WssLoggerLogDto
            {
                Level = level,
                Message = message,
                Category = Category,
                Timestamp = DateTimeOffset.UtcNow,
                StackTrace = exception?.ToString()
            };

            await hub.Send_Log_ToServerAsync(dto, default);
        });
    }

    // Kleine private scope-implementatie
    private sealed class Scope(IDictionary<string, object?> state) : IDisposable
    {
        public IDictionary<string, object?> State { get; } = state;

        public void Dispose()
        {
            // geen-op, maar kan later uitbreiden
        }
    }
}

