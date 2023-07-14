using System;
using System.Security.Claims;
using System.Threading;

namespace Conqueror.Common.Middleware.Authentication;

internal sealed class ConquerorAuthenticationContext : IConquerorAuthenticationContext
{
    private static readonly AsyncLocal<ConquerorAuthenticationContextHolder> ConquerorAuthenticationContextCurrent = new();

    public IDisposable SetCurrentPrincipal(ClaimsPrincipal? principal)
    {
        // use an object indirection to hold the principal in the AsyncLocal,
        // so it can be cleared in all ExecutionContexts when its cleared.
        ConquerorAuthenticationContextCurrent.Value = new() { Principal = principal };

        return new AnonymousDisposable(ClearPrincipal);
    }

    public ClaimsPrincipal? GetCurrentPrincipal() => ConquerorAuthenticationContextCurrent.Value?.Principal;

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

    private sealed class AnonymousDisposable : IDisposable
    {
        private readonly Action onDispose;

        public AnonymousDisposable(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose() => onDispose();
    }
}
