namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

internal sealed class HttpEndpointActionDescriptorChangeProvider : IActionDescriptorChangeProvider, IDisposable
{
    private CancellationTokenSource? cancellationTokenSource;

    public IChangeToken GetChangeToken()
    {
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new CancellationTokenSource();

        return new CancellationChangeToken(cancellationTokenSource.Token);
    }

    public void Dispose() => cancellationTokenSource?.Dispose();

    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "cannot be made async"
    )]
    public void Signal() => cancellationTokenSource?.Cancel();
}
