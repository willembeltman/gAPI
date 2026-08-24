namespace gAPI.Core.Server.Authentication;

public sealed class RequestIds
{
    public long SessionId { get; set; }
    public long RouteId { get; set; }
    public long UserIpId { get; set; }
    public long UserIpSessionId { get; set; }
    public long UserIpSessionTokenId { get; set; }
    public long UserIpSessionTokenRouteId { get; set; }
    public long UserIpSessionTokenRouteRequestId { get; set; }
    public int Counter { get; set; }
}