namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestDone,
    SendRequestCancelled,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeArgumentCancelled,
    InvokeRequest,
    InvokeRequestCancelled,
    InvokeResponse,
    InvokeResponseDone,

    Log
}