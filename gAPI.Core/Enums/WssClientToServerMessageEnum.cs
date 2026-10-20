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
    FabricInvokeRequestAck,
    FabricInvokeRequestDone,

    StreamingRequest,
    StreamingResponse,

    FabricStreamingRequest,
    FabricStreamingResponse,

    Log
}