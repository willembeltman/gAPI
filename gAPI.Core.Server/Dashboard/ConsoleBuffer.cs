using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Dashboard;

public class ConsoleBuffer : ILoggerProvider
{
    public Channel<ColorLine> Lines { get; } = Channel.CreateUnbounded<ColorLine>();

    // Bestaande methoden voor handmatige writes
    public void WriteError(string message, Exception ex)
    {
        Lines.Writer.TryWrite(new ColorLine() { Color = ConsoleColor.Red, Text = $"{message}\r\n{ex?.Message}" });
    }

    public void WriteInfo(string message)
    {
        Lines.Writer.TryWrite(new ColorLine() { Color = ConsoleColor.White, Text = message });
    }

    // ILoggerProvider implementatie
    public ILogger CreateLogger(string categoryName)
    {
        return new BufferLogger(this, categoryName);
    }

    public void Dispose()
    {
        // Niets te disposen voor de ConcurrentQueue
    }

    // Interne ILogger klasse die naar de buffer schrijft
    private class BufferLogger : ILogger
    {
        private readonly ConsoleBuffer _buffer;
        private readonly string _categoryName;

        public BufferLogger(ConsoleBuffer buffer, string categoryName)
        {
            _buffer = buffer;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) 
            where TState : notnull
        {
            return null!;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            // Formatteer het bericht (inclusief eventuele log-parameters)
            string message = formatter(state, exception);
            string fullMessage = $"[{logLevel}] [{_categoryName}] {message}";

            if (logLevel >= LogLevel.Error)
            {
                _buffer.WriteError(fullMessage, exception);
            }
            else
            {
                // Pas eventueel de kleur aan op basis van LogLevel (bijv. Yellow voor Warning)
                _buffer.WriteInfo(fullMessage);
            }
        }
    }
}

