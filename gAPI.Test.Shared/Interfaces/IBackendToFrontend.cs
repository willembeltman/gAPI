using gAPI.Attributes;

namespace gAPI.Test.Shared.Interfaces;

[GenerateHub]
public interface IBackendToFrontend
{
    Task Notify(string message);
}
