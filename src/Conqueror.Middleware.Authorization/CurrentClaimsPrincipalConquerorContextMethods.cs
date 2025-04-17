using System;
using System.Security.Claims;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     For linking into other projects.
/// </summary>
internal static class CurrentClaimsPrincipalConquerorContextMethods
{
    private const string CurrentPrincipalKey = "conqueror-middleware-authorization-current-principal";

    public static IDisposable SetCurrentPrincipalInternal(this ConquerorContext conquerorContext, ClaimsPrincipal principal)
    {
        conquerorContext.DownstreamContextData.Set(CurrentPrincipalKey, principal);
        return new AnonymousDisposable(conquerorContext.ClearCurrentPrincipalInternal);
    }

    public static void ClearCurrentPrincipalInternal(this ConquerorContext conquerorContext)
    {
        _ = conquerorContext.DownstreamContextData.Remove(CurrentPrincipalKey);
    }

    public static ClaimsPrincipal? GetCurrentPrincipalInternal(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<ClaimsPrincipal>(CurrentPrincipalKey);
    }

    private sealed class AnonymousDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
