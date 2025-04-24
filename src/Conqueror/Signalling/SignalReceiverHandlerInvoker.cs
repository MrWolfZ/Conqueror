using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Signalling;

internal sealed class SignalReceiverHandlerInvoker<TTypesInjector>(
    SignalHandlerRegistration registration,
    TTypesInjector typesInjector)
    : ISignalReceiverHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, ISignalHandlerTypesInjector
{
    public Type SignalType { get; } = registration.SignalType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public Task Invoke<TSignal>(TSignal signal,
                                IServiceProvider serviceProvider,
                                string transportTypeName,
                                string? traceId,
                                IEnumerable<string> encodedContextData,
                                CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        if (traceId is not null)
        {
            conquerorContext.SetTraceId(traceId);
        }

        conquerorContext.DecodeContextData(encodedContextData);

        return registration.Invoker.Invoke(serviceProvider, signal, transportTypeName, cancellationToken);
    }
}
