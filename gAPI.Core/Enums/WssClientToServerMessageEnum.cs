namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,
    SendRequest,
    SendArgumentedRequest,
    SendArgumentedRequestDone,
    InvokeRequest,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeResponse,
    InvokeResponseDone,
    Log
}