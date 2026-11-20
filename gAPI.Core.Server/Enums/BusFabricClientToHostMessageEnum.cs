namespace gAPI.Core.Server.Enums;

public enum BusFabricClientToHostMessageEnum
{
    BusSubscribe,
    BusUnsubscribe,

    BusFireAndForget,

    BusSendRequest,
    BusSendRequestCancelled,
    BusSendRequestDone,

    BusInvokeRequest,
    BusInvokeRequestCancelled,
    BusInvokeRequestAck,
    BusInvokeRequestDone,

    BusStreamingRequest,
    BusStreamingResponse,
}