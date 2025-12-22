using gAPI.Attributes;

namespace gAPI.Test.Shared.Interfaces;

[GenerateApi]
public interface IFrontendToBackend
{
    Task NotifyToAll(string message);
    Task<string> Hello(string message);
    Task<int> World(string message);
}