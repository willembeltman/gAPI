namespace UwvLlm.Infrastructure.Messaging.Interfaces;

public interface IConsoleService
{
    Task Start(CancellationToken ct);
    void WriteLine(Exception ex);
    void WriteLine(string text);
}