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
    BusSendRequestAck,

    BusInvokeRequest,
    BusInvokeRequestCancelled,
    BusInvokeRequestAck,

    BusStreamingRequest_ResponseEnumerable,
    BusStreamingResponse_ResponseEnumerable,
    BusStreamingRequest_ArgumentEnumerable,
    BusStreamingResponse_ArgumentEnumerable
}