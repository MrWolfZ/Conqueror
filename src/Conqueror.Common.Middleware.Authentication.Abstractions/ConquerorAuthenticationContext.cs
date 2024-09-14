using System;
using System.Security.Claims;
using System.Threading;

namespace Conqueror;

/// <summary>
///     Provides access to the currently authenticated principal.
/// </summary>
public sealed class ConquerorAuthenticationContext
{
    private static readonly AsyncLocal<ConquerorAuthenticationContextHolder> ConquerorAuthenticationContextCurrent = new();

    /// <summary>
    ///     Set the currently authenticated principal. When passing <c>null</c>, the
    ///     currently authenticated principal (if any) is cleared from the context.
    /// </summary>
    /// <param name="principal">The principal to set</param>
    /// <returns>A disposable which can be disposed to clear the principal</returns>
    public IDisposable SetCurrentPrincipal(ClaimsPrincipal? principal)
    {
        // use an object indirection to hold the principal in the AsyncLocal,
        // so it can be cleared in all ExecutionContexts when it's cleared.
        ConquerorAuthenticationContextCurrent.Value = new() { Principal = principal };

        return new AnonymousDisposable(ClearPrincipal);
    }

    /// <summary>
    ///     The currently authenticated principal, or <c>null</c> if no principal is set.
    /// </summary>
    public ClaimsPrincipal? CurrentPrincipal => ConquerorAuthenticationContextCurrent.Value?.Principal;

    private static void ClearPrincipal()
    {
        var holder = ConquerorAuthenticationContextCurrent.Value;

        if (holder != null)
        {
            // clear current principal trapped in the AsyncLocals, as it's done.
            holder.Principal = null;
        }
    }

    private sealed class ConquerorAuthenticationContextHolder
    {
        public ClaimsPrincipal? Principal { get; set; }
    }

    private sealed class AnonymousDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
