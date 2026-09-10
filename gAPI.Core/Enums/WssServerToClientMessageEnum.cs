namespace gAPI.Core.Enums;

public enum WssServerToClientMessageEnum
{
    SynchronizeClientIds,
    
    SendRequest,
    SendRequestCancelled,
    SendRequestDone,

    InvokeRequest,
    InvokeRequestCancelled,
    InvokeRequestDone,

    StreamingRequest,
    StreamingResponse
}