using System;
using System.Security.Claims;

namespace Conqueror;

/// <summary>
///     Provides access to the currently authenticated principal.
/// </summary>
public interface IConquerorAuthenticationContext
{
    /// <summary>
    ///     Set the currently authenticated principal. When passing <c>null</c>, the
    ///     currently authenticated principal (if any) is cleared from the context.
    /// </summary>
    /// <param name="principal">The principal to set</param>
    /// <returns>A disposable which can be disposed to clear the principal</returns>
    IDisposable SetCurrentPrincipal(ClaimsPrincipal? principal);

    /// <summary>
    ///     Get the currently authenticated principal (if any).
    /// </summary>
    /// <returns>The currently authenticated principal, or <c>null</c> if no principal is set</returns>
    ClaimsPrincipal? GetCurrentPrincipal();
}
