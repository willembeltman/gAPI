using gAPI.Test.Shared.Interfaces;

namespace gAPI.Test.Backend.Services
{
    public class HelloWorldService : IHelloWorldService
    {
        public string Hello(string message)
        {
            return message;
        }
        public int World(string message, KeyValuePair<int, string> test)
        {
            return 26;
        }
    }
}
