namespace Conqueror.Signalling;

internal sealed class SignalReceivers(IServiceProvider serviceProvider) : ISignalReceivers
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ReceiverExecutionHandle RunReceivers<TTypesInjector, TReceiver>(
        ISignalReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        ISignalReceiverRunner<TReceiver> receiverRunner,
        CancellationToken cancellationToken
    )
        where TTypesInjector : class, ISignalHandlerTypesInjector
        where TReceiver : class
    {
        var registry = ServiceProvider.GetRequiredService<ISignalHandlerRegistry>();
        var invokers = registry.GetReceiverHandlerInvokers<TTypesInjector>();

        var configuredHandlerTypes = new HashSet<Type>();

        var configuredReceivers = invokers
            .GroupBy(i => i.HandlerType)
            .SelectMany(g =>
            {
                var handlerType = g.Key;
                var invokersForHandlerType = g.ToList();
                var typesInjector = invokersForHandlerType[0].TypesInjector;

                // for delegate handlers
                if (handlerType is null)
                {
                    return invokersForHandlerType.Select(i =>
                        CreateReceiverForHandlerType(receiverFactory, handlerType: null, [i], typesInjector)
                    );
                }

                if (!configuredHandlerTypes.Add(handlerType))
                {
                    // if the receiver was already configured, we can skip it
                    return [];
                }

                return
                [
                    CreateReceiverForHandlerType(receiverFactory, handlerType, invokersForHandlerType, typesInjector),
                ];
            })
            .OfType<(TReceiver Receiver, Type? HandlerType)>()
            .ToList();

        return CombineExecutions(
            configuredReceivers.ConvertAll(t =>
                RunReceiver(
                    receiverRunner,
                    t.Receiver,
                    t.HandlerType,
                    receiverFactory.TransportTypeName,
                    cancellationToken
                )
            )
        );
    }

    public ReceiverExecutionHandle RunReceiver<THandler, TTypesInjector, TReceiver>(
        ISignalReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        ISignalReceiverRunner<TReceiver> receiverRunner,
        CancellationToken cancellationToken
    )
        where TTypesInjector : class, ISignalHandlerTypesInjector
        where TReceiver : class
    {
        var registry = ServiceProvider.GetRequiredService<ISignalHandlerRegistry>();
        var invokersForHandlerType = registry
            .GetReceiverHandlerInvokers<TTypesInjector>()
            .Where(i => i.HandlerType == typeof(THandler))
            .ToList();
        var typesInjector =
            invokersForHandlerType.FirstOrDefault()?.TypesInjector
            ?? throw new InvalidOperationException(
                $"did not find registrations for signal handler type {typeof(THandler)}"
            );

        var receiver = CreateReceiverForHandlerType(
            receiverFactory,
            typeof(THandler),
            invokersForHandlerType,
            typesInjector
        )?.Receiver;

        if (receiver is null)
        {
            return new ReceiverExecutionHandle(
                Task.CompletedTask,
                Task.CompletedTask,
                cancellationTokenSource: null,
                onDispose: null
            );
        }

        return RunReceiver(
            receiverRunner,
            receiver,
            typeof(THandler),
            receiverFactory.TransportTypeName,
            cancellationToken
        );
    }

    public ReceiverExecutionHandle CombineExecutions(IReadOnlyCollection<ReceiverExecutionHandle> executionHandles)
    {
        var combinedRun = new ReceiverExecutionHandle(executionHandles);
        HandleErrors(combinedRun);

        return combinedRun;

        [SuppressMessage("Design", "MA0155:Do not use async void methods", Justification = "we are making it safe")]
        static async void HandleErrors(ReceiverExecutionHandle combinedExecutionHandle)
        {
            try
            {
                // at this point all receivers are running, so we can observe their completion tasks for any errors,
                // in which case we cancel all remaining executions; this in turn will cancel the overall execution
                // and expose the error to the caller through the completion task of the returned handle
                var executionsToObserve = combinedExecutionHandle.InnerHandles!.ToList();

                while (executionsToObserve.Count > 0)
                {
                    var completedTask = await Task.WhenAny(executionsToObserve.Select(r => r.CompletionTask))
                        .ConfigureAwait(false);

                    if (completedTask.IsFaulted)
                    {
                        // this will trigger the `finally` block below, which will cancel all remaining executions
                        return;
                    }

                    _ = executionsToObserve.Remove(executionsToObserve.First(r => r.CompletionTask == completedTask));
                }
            }
            catch
            {
                // this should never happen (i.e. there is no known circumstance under which
                // the block above throws), but if it does, we want to cancel all remaining executions
            }
            finally
            {
                // cancel all remaining executions
                await combinedExecutionHandle.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static (TReceiver Receiver, Type? HandlerType)? CreateReceiverForHandlerType<TTypesInjector, TReceiver>(
        ISignalReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        Type? handlerType,
        IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> invokersForHandlerType,
        TTypesInjector typesInjector
    )
        where TTypesInjector : class, ISignalHandlerTypesInjector
        where TReceiver : class
    {
        try
        {
            var receiver = receiverFactory.CreateReceiverForHandlerType(
                handlerType,
                invokersForHandlerType,
                typesInjector
            );

            return receiver is null ? null : (receiver, handlerType);
        }
        catch (Exception ex)
        {
            throw new SignalReceiverExecutionFailedException(
                $"failed to run the signal receiver for handler type '{handlerType}'",
                ex
            )
            {
                HandlerType = handlerType,
                SignalTransportType = new SignalTransportType(
                    receiverFactory.TransportTypeName,
                    SignalTransportRole.Receiver
                ),
            };
        }
    }

    [SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "false positive")]
    private static ReceiverExecutionHandle RunReceiver<TReceiver>(
        ISignalReceiverRunner<TReceiver> receiverRunner,
        TReceiver receiver,
        Type? handlerType,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TReceiver : class
    {
        try
        {
            return receiverRunner.RunReceiver(receiver, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new SignalReceiverExecutionFailedException(
                $"failed to run the signal receiver for handler type '{handlerType}'",
                ex
            )
            {
                HandlerType = handlerType,
                SignalTransportType = new SignalTransportType(transportTypeName, SignalTransportRole.Receiver),
            };
        }
    }
}
