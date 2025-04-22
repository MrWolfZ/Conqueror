using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class DelegateSignalHandler<TSignal>(
    SignalHandlerFn<TSignal> handlerFn,
    IServiceProvider serviceProvider)
    : ISignalHandler<TSignal, DelegateSignalHandler<TSignal>>
    where TSignal : class, ISignal<TSignal>
{
    public async Task Handle(TSignal signal, CancellationToken cancellationToken = default)
    {
        await handlerFn(signal, serviceProvider, cancellationToken).ConfigureAwait(false);
    }
}
