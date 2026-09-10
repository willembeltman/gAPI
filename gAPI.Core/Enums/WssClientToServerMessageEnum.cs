namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestCancelled,
    SendRequestDone,

    InvokeRequest,
    InvokeRequestCancelled,
    InvokeRequestDone,

    StreamingRequest,
    StreamingResponse,

    Log
}