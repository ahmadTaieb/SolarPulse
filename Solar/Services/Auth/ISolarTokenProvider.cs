using System.Threading;
using System.Threading.Tasks;

namespace Solar.Services.Auth;

/// <summary>
/// Provides thread-safe token management and authentication for the Solar cloud platform.
/// </summary>
public interface ISolarTokenProvider
{
    /// <summary>
    /// Obtains a valid access token, performing authentication or refresh if required.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the currently cached token to trigger re-authentication on the next request.
    /// </summary>
    void InvalidateToken();
}
