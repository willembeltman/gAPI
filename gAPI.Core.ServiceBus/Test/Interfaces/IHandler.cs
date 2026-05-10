namespace UwvLlm.Infrastructure.Messaging.Interfaces;

public interface IHandler
{
}
public interface IHandler<TMessage> : IHandler
{
    Task Handle(TMessage message, CancellationToken ct);
}
