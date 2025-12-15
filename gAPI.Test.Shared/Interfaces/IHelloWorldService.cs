using gAPI.Attributes;

namespace gAPI.Test.Shared.Interfaces;

[Generate]
public interface IHelloWorldService
{
    string Hello(string message);
    int World(string message, KeyValuePair<int, string> test);
}