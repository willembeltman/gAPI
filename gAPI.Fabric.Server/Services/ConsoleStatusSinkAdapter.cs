using gAPI.Fabric.Server.Interfaces;

namespace gAPI.Fabric.Server.Services;

internal sealed class ConsoleStatusSinkAdapter(IConsole console) : IFabricStatusSink
{
    public void WriteInfo(string message) => console.WriteLine(message);

    public void WriteError(string message, Exception? exception = null)
    {
        console.WriteLine(message, ConsoleColor.Red);
        if (exception is not null)
            console.WriteLine(exception.ToString(), ConsoleColor.Red);
    }
}
