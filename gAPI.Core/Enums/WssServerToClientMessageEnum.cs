namespace gAPI.Core.Enums;

public enum WssServerToClientMessageEnum
{
    SynchronizeClientIds,
    SendRequest,
    SendRequestDone,
    SendRequestCancelled,
    StreamingRequest,
    StreamingResponse,
    InvokeRequest,
    InvokeCancelled,
    InvokeResponse,
    InvokeRequestDone
}