using System;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointActionDescriptorChangeProvider : IActionDescriptorChangeProvider, IDisposable
{
    private CancellationTokenSource? cancellationTokenSource;

    public IChangeToken GetChangeToken()
    {
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = new();
        return new CancellationChangeToken(cancellationTokenSource.Token);
    }

    public void Signal()
    {
        cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        cancellationTokenSource?.Dispose();
    }
}
