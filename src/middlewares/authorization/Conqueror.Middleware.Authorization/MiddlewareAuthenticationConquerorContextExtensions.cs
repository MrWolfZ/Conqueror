using System;
using System.Security.Claims;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     Extension methods which provide access to the currently authenticated principal.
/// </summary>
public static class MiddlewareAuthenticationConquerorContextExtensions
{
    /// <summary>
    ///     Set the currently authenticated principal. When passing <c>null</c>, the
    ///     currently authenticated principal (if any) is cleared from the context.
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to set the principal in</param>
    /// <param name="principal">The principal to set</param>
    /// <returns>A disposable which can be disposed to clear the principal</returns>
    public static IDisposable SetCurrentPrincipal(this ConquerorContext conquerorContext, ClaimsPrincipal principal)
        => conquerorContext.SetCurrentPrincipalInternal(principal);

    /// <summary>
    ///     Set the currently authenticated principal. When passing <c>null</c>, the
    ///     currently authenticated principal (if any) is cleared from the context.
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to set the principal in</param>
    public static void ClearCurrentPrincipal(this ConquerorContext conquerorContext)
        => conquerorContext.ClearCurrentPrincipalInternal();

    /// <summary>
    ///     Get the currently authenticated principal, or <c>null</c> if no principal is set.
    /// </summary>
    /// <param name="conquerorContext">The Conqueror context to get the principal from</param>
    /// <returns>The currently authenticated principal, or <c>null</c> if no principal is set</returns>
    public static ClaimsPrincipal? GetCurrentPrincipal(this ConquerorContext conquerorContext)
        => conquerorContext.GetCurrentPrincipalInternal();
}
