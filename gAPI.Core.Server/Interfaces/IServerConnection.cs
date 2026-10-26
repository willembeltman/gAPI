namespace gAPI.Core.Server.Interfaces;

public interface IServerConnection : ISpeedGetter
{
    string Id { get; }
}
