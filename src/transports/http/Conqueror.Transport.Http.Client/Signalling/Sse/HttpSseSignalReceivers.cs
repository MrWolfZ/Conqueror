using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceivers : IHttpSseSignalReceivers
{
    public ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        return receivers.RunReceivers(
            receivers.ServiceProvider.GetRequiredService<HttpSseSignalReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<HttpSseSignalReceiverRunner>(),
            cancellationToken);
    }

    public ReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler, ISignalHandlerWithSourceGeneration
    {
        return receivers.RunReceiver<THandler, IHttpSseSignalHandlerTypesInjector, HttpSseSignalReceiver>(
            receivers.ServiceProvider.GetRequiredService<HttpSseSignalReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<HttpSseSignalReceiverRunner>(),
            cancellationToken);
    }
}
