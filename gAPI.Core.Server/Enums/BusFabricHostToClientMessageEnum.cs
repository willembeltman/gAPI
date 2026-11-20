namespace gAPI.Core.Server.Enums;

public enum BusFabricHostToClientMessageEnum
{
    SynchronizeFabricIds,

    SubscribeAck,
    UnsubscribeAck,

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