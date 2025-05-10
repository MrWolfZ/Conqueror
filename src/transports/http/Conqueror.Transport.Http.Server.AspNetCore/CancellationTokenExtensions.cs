using System;
using System.Threading;

namespace Conqueror.Transport.Http.Server.AspNetCore;

internal static class CancellationTokenExtensions
{
    public static CancellationTokenRegistration Register<TState>(this CancellationToken cancellationToken, Action<TState> action, TState state)
        => cancellationToken.Register(s => action((TState)s!), state);
}
