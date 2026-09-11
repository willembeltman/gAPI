namespace gAPI.Core.Enums;

public enum WssServerToClientMessageEnum
{
    SynchronizeClientIds,
    
    FabricSendRequest,
    FabricSendRequestCancelled,
    SendRequestDone,

    FabricInvokeRequest,
    FabricInvokeRequestCancelled,
    InvokeRequestDone,

    StreamingRequest,
    StreamingResponse,

    FabricStreamingRequest,
    FabricStreamingResponse
}