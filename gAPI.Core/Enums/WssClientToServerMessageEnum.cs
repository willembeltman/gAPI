namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestDone,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeRequest,
    InvokeResponse,
    InvokeResponseDone,

    Log
}