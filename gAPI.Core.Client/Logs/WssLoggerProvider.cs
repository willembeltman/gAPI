using gAPI.Core.Dtos;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace gAPI.Core.Client.Logs;

public sealed class WssLoggerProvider(ClientLogQueue queue)
    : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider =
        new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return new WssLogger(
            categoryName,
            queue,
            () => Volatile.Read(ref _scopeProvider));
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        Volatile.Write(ref _scopeProvider, scopeProvider);
    }

    public void Dispose()
    {
    }

    private sealed class WssLogger(
        string category,
        ClientLogQueue queue,
        Func<IExternalScopeProvider> getScopeProvider)
        : ILogger
    {
        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => getScopeProvider().Push(state);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                var properties = new Dictionary<string, string?>();
                string? messageTemplate = null;

                // Structured logging properties
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    foreach (var pair in values)
                    {
                        if (pair.Key == "{OriginalFormat}")
                        {
                            messageTemplate = pair.Value?.ToString();
                            continue;
                        }

                        properties[pair.Key] = pair.Value?.ToString();
                    }
                }

                // Active logging scopes
                var scopes = new List<string>();

                getScopeProvider().ForEachScope(
                    (scope, list) =>
                    {
                        if (scope is IEnumerable<KeyValuePair<string, object?>> scopeValues)
                        {
                            list.Add(string.Join(", ",
                                scopeValues.Select(x => $"{x.Key}={x.Value}")));
                        }
                        else if (scope != null)
                        {
                            list.Add(scope.ToString() ?? string.Empty);
                        }
                    },
                    scopes);

                var activity = Activity.Current;

                var dto = new LogDto
                {
                    Level = logLevel,
                    Message = formatter(state, exception),

                    Category = category,
                    Source = "Client",

                    Timestamp = DateTimeOffset.UtcNow,

                    EventId = eventId.Id,
                    EventName = eventId.Name,

                    MessageTemplate = messageTemplate,
                    Properties = properties,
                    Scopes = scopes.ToArray(),

                    CorrelationId = activity?.TraceId.ToString(),

                    ExceptionDetails = exception?.ToString()
                };

                // Geen netwerkverkeer of async werk vanuit ILogger.Log().
                queue.TryWrite(dto);
            }
            catch
            {
                // Logging mag de applicatie nooit laten crashen.
            }
        }
    }
}

