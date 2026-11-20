using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Example.Shared.ApiPublic;

[GenerateApi]
public interface IApiApi
{
    Task StartServerTests(double time, CancellationToken ct);

    void SendAndForget(string message, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task<string> InvokeTask(string name, CancellationToken ct);
    void Ping(string v);
    void Pong(string v);
}
