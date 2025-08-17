#pragma warning disable IDE0130 // Namespaces don't match folder structure - part of the public API

namespace Conqueror;

using System.Security.Claims;

/// <summary>
///     For linking into other projects.
/// </summary>
internal static class CurrentClaimsPrincipalConquerorContextMethods
{
    public static IDisposable SetCurrentPrincipalInternal(
        this ConquerorContext conquerorContext,
        ClaimsPrincipal principal
    )
    {
        conquerorContext.CurrentPrincipal = principal;

        return new AnonymousDisposable(conquerorContext.ClearCurrentPrincipalInternal);
    }

    public static void ClearCurrentPrincipalInternal(this ConquerorContext conquerorContext) =>
        conquerorContext.CurrentPrincipal = null;

    private sealed class AnonymousDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
