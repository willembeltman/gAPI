namespace gAPI.Interfaces;

public interface IServerAuthenticationService
{
    /// <summary>
    /// Is called to see if the request is authenticated. Return true if client is authenticated, otherwise return false.
    /// </summary>
    /// <param name="sessionId">The current sessionid, REQUIRED</param>
    /// <returns>If the user is logged in</returns>
    Task<bool> InitializeAsync(string sessionId);
    /// <summary>
    /// Gets a value indicating whether access to the resource is forbidden.
    /// WARNING: Throws when not initialized.
    /// </summary>
    bool IsForbidden { get; }
    /// <summary>
    /// Gets the unique identifier that defines the scope for the current context.
    /// If authentication service is not initialized, this value will be null.
    /// WARNING: Throws when not initialized.
    /// </summary>
    string SessionId { get; }
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// If authentication service is not initialized, this value will be null.
    /// Or, if the user is not authenticated, this value will be null.
    /// WARNING: Throws when not initialized.
    /// </summary>
    string? UserId { get; }
}