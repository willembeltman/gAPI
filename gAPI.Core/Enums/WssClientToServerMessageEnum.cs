namespace gAPI.Core.Enums;

public enum WssClientToServerMessageEnum
{
    Initialize,
    Subscribe,
    Unsubscribe,

    SendAndForgetRequest,

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

    Log,
    Ping
}