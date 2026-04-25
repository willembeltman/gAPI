using gAPI.Ids;

namespace gAPI.Sse;

public class HubResult
{
    /// <summary>
    /// Not required, will be set by the framework
    /// </summary>
    public UserId UserId { get; set; }

    /// <summary>
    /// Not required, will be set by the framework
    /// </summary>
    public SessionId SessionId { get; set; }

    /// <summary>
    /// Optional, can be used to return state/session data after an invoke
    /// </summary>
    public string? ControlName { get; set; }

    /// <summary>
    /// Optional, can be used to return state/session data after an invoke
    /// </summary>
    public string? ExtraInfo { get; set; }
}
