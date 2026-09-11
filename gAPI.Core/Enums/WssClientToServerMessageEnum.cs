namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestCancelled,
    FabricSendRequestDone,

    InvokeRequest,
    InvokeRequestCancelled,
    FabricInvokeRequestDone,

    StreamingRequest,
    StreamingResponse,

    FabricStreamingRequest,
    FabricStreamingResponse,

    Log
}