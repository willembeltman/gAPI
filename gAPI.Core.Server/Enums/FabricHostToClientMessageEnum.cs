namespace gAPI.Core.Server.Enums;

public enum FabricHostToClientMessageEnum
{
    GetSessionCookieDataResponse,
    SynchronizeFabricIds,
    Log,


    FabricSendAndForgetRequest,
    FabricSendAndForgetRequestCancelled,

    FabricSendRequest,
    FabricSendRequestCancelled,
    SendRequestDone,

    FabricInvokeRequest,
    FabricInvokeRequestCancelled,
    InvokeRequestAck,
    InvokeRequestDone,

    StreamingRequestServerToClient,
    StreamingResponseServerToClient,
    StreamingRequestClientToServer,
    StreamingResponseClientToServer,


    BusSubscribeAck,
    BusUnsubscribeAck,

    BusFireAndForget,
    BusFireAndForgetCancelled,

    BusSendRequest,
    BusSendRequestCancelled,
    BusSendRequestDone,

    BusInvokeRequest,
    BusInvokeRequestCancelled,
    BusInvokeRequestAck,
    BusInvokeRequestDone,

    BusStreamingRequest,
    BusStreamingResponse
}