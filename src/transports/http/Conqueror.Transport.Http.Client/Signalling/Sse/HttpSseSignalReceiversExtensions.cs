using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Transport.Http.Client.Signalling.Sse;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpSseSignalReceiversExtensions
{
    public static IAsyncDisposable RunHttpSseSignalReceivers(this ISignalReceivers receivers)
    {
        var cts = new CancellationTokenSource();
        var runTask = receivers.RunHttpSseSignalReceivers(cts.Token);

        return new AnonymousAsyncDisposable(async () =>
        {
            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
            await runTask.ConfigureAwait(false);
        });
    }

    public static async Task RunHttpSseSignalReceivers(this ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        var registry = receivers.ServiceProvider.GetRequiredService<ISignalHandlerRegistry>();
        var invokers = registry.GetReceiverHandlerInvokers<IHttpSseSignalHandlerTypesInjector>();
        var injectable = new Injectable(receivers, cancellationToken);

        await Task.WhenAll(invokers.Select(i => i.TypesInjector.Create(injectable))).ConfigureAwait(false);
    }

    public static IAsyncDisposable RunHttpSseSignalReceiver<THandler>(this ISignalReceivers receivers)
        where THandler : class, IHttpSseSignalHandler
    {
        var cts = new CancellationTokenSource();
        var runTask = receivers.RunHttpSseSignalReceiver<THandler>(cts.Token);

        return new AnonymousAsyncDisposable(async () =>
        {
            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
            await runTask.ConfigureAwait(false);
        });
    }

    public static Task RunHttpSseSignalReceiver<THandler>(this ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler
    {
        var singletons = receivers.ServiceProvider.GetRequiredService<ConquerorSingletons>();
        var runner = singletons.GetOrAddSingleton(p => new HttpSseSignalReceiversRunner(p));

        return runner.RunHttpSseSignalReceiver<THandler>(cancellationToken);
    }

    private sealed class Injectable(
        ISignalReceivers receivers,
        CancellationToken cancellationToken)
        : IHttpSseSignalTypesInjectable<Task>
    {
        private readonly HashSet<Type> processedHandlerTypes = new();

        Task IHttpSseSignalTypesInjectable<Task>.WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            if (!processedHandlerTypes.Add(typeof(THandler)))
            {
                return Task.CompletedTask;
            }

            return receivers.RunHttpSseSignalReceiver<THandler>(cancellationToken);
        }
    }

    private sealed class AnonymousAsyncDisposable(Func<Task> onDispose) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await onDispose().ConfigureAwait(false);
        }
    }
}
