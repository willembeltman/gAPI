using System;
using System.Threading.Tasks;

namespace gAPI.Interfaces
{
    public interface IServerAuthenticationService
    {
        /// <summary>
        /// Is called to see if the request is authenticated. Return true if client is authenticated, otherwise return false.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        Task<bool> InitializeAsync(Guid sessionId);
        /// <summary>
        /// Asynchronously retrieves the unique identifier of the current user, if available.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user identifier as a string,
        /// or <see langword="null"/> if no user is authenticated.</returns>
        Task<string?> GetUserId();
        /// <summary>
        /// Gets the unique identifier that defines the scope for the current context, if available.
        /// </summary>
        Guid? SessionId { get; }
    }
}