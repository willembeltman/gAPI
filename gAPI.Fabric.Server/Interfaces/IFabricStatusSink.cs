namespace gAPI.Fabric.Server.Interfaces;

/// <summary>
/// Receives operational messages from the Fabric runtime without coupling it to a UI.
/// </summary>
public interface IFabricStatusSink
{
    void WriteInfo(string message);
    void WriteError(string message, Exception? exception = null);
}
