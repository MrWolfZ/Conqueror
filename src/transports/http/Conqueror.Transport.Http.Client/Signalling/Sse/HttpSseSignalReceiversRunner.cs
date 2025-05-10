using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiversRunner(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<Type, IHttpSseSignalReceiverRunner> runnerByHandlerType = new();

    // TODO: add test for duplicate run
    public Task RunHttpSseSignalReceiver<THandler>(CancellationToken cancellationToken)
        where THandler : class, IHttpSseSignalHandler
    {
        var runner = new HttpSseSignalReceiverRunner<THandler>(serviceProvider);
        if (!runnerByHandlerType.TryAdd(typeof(THandler), runner))
        {
            throw new InvalidOperationException($"the receiver for handler type '{typeof(THandler)}' is already running");
        }

        return runner.Run(cancellationToken);
    }
}
