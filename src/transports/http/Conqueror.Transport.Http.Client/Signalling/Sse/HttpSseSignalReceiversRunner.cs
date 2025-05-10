using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiversRunner(IServiceProvider serviceProvider)
{
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, the source is returned to the caller")]
    public SignalReceiverRun Run<THandler>(CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler
    {
        try
        {
            var receiver = new HttpSseSignalReceiver(serviceProvider);
            THandler.ConfigureHttpSseReceiver(receiver);

            if (!receiver.IsEnabled)
            {
                return new(Task.CompletedTask, null);
            }

            foreach (var invoker in serviceProvider.GetRequiredService<ISignalHandlerRegistry>()
                                                   .GetReceiverHandlerInvokers<IHttpSseSignalHandlerTypesInjector>()
                                                   .Where(i => i.HandlerType == typeof(THandler)))
            {
                _ = invoker.TypesInjector.Create(new ConfigurationInjectable(invoker, receiver));
            }

            var runner = new HttpSseSignalReceiverRunner(
                receiver,
                serviceProvider.GetRequiredService<IConquerorContextAccessor>());

            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            return new(runner.Run(typeof(THandler), linkedSource.Token), linkedSource);
        }
        catch (Exception ex)
        {
            throw new HttpSseSignalReceiverRunFailedException($"failed to run the signal receiver for handler type '{typeof(THandler)}'", ex)
            {
                HandlerType = typeof(THandler),
            };
        }
    }

    private sealed class ConfigurationInjectable(ISignalReceiverHandlerInvoker invoker, HttpSseSignalReceiver receiver) : IHttpSseSignalTypesInjectable<object?>
    {
        object? IHttpSseSignalTypesInjectable<object?>.WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            receiver.AddSignalType<TSignal>(invoker);

            return null;
        }
    }
}
