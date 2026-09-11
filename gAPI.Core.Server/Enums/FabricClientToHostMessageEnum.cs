namespace gAPI.Core.Server.Enums;

public enum FabricClientToHostMessageEnum
{
    Subscribe,
    Unsubscribe,

    SendRequest,
    SendRequestCancelled,
    SendRequestDone,

    InvokeRequest,
    InvokeRequestCancelled,
    InvokeRequestDone,

    StreamingRequestServerToClient,
    StreamingResponseServerToClient,
    StreamingRequestClientToServer,
    StreamingResponseClientToServer,

    UpdateSession,
    ClearSession,
    GetSessionCookieData
}