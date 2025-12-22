using gAPI.Test.Api.HubServices;
using gAPI.Test.Shared.Interfaces;

namespace gAPI.Test.Api.Services;

public class FrontendToBackend(ISignalRContext SignalR) : IFrontendToBackend
{
    public Task<string> Hello(string message)
    {
        return Task.FromResult(message);
    }

    public async Task NotifyToAll(string message)
    {
        await SignalR.BackendToFrontend.ToAll.Notify(message);
    }

    public Task<int> World(string message)
    {
        return Task.FromResult(26);
    }
}
