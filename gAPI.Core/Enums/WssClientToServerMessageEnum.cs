namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,
    SendRequest,
    InvokeRequest,
    InvokeArgumentRequest,
    InvokeArgumentResponse,
    InvokeResponse,
    InvokeResponseDone,
    Log
}