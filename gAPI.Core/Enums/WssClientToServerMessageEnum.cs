namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestDone,
    SendRequestCancelled,
    StreamingRequest,
    StreamingResponse,

    InvokeRequest,
    InvokeRequestCancelled,
    //InvokeResponse,
    InvokeRequestDone,

    Log
}