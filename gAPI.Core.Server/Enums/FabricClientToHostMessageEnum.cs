namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,
    SendRequest,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone
}