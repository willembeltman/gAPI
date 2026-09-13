namespace gAPI.Core.Helpers;

public class RemoteException : Exception
{
    public RemoteException(string? message) : base(message)
    {
    }
}
