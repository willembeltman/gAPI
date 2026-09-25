using gAPI.Fabric.Server.Interfaces;

namespace gAPI.Fabric.Server.Services;

internal sealed class NullFabricStatusSink : IFabricStatusSink
{
    public static NullFabricStatusSink Instance { get; } = new();

    public void WriteInfo(string message)
    {
    }

    public void WriteError(string message, Exception? exception = null)
    {
    }
}
