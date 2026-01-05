namespace Conqueror.Iterating;

internal sealed class IteratorServers(IServiceProvider serviceProvider) : IIteratorServers
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ReceiverExecutionHandle RunServers<TTypesInjector, TServer>(
        IIteratorServerFactory<TTypesInjector, TServer> serverFactory,
        IIteratorServerRunner<TServer> serverRunner,
        CancellationToken cancellationToken
    )
        where TTypesInjector : class, IIteratorHandlerTypesInjector
        where TServer : class
    {
        var registry = ServiceProvider.GetRequiredService<IIteratorHandlerRegistry>();
        var invokers = registry.GetServerHandlerInvokers<TTypesInjector>();

        var configuredHandlerTypes = new HashSet<Type>();

        var configuredServers = invokers
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
                        CreateServerForHandlerType(serverFactory, handlerType: null, [i], typesInjector)
                    );
                }

                if (!configuredHandlerTypes.Add(handlerType))
                {
                    // if the server was already configured, we can skip it
                    return [];
                }

                return [CreateServerForHandlerType(serverFactory, handlerType, invokersForHandlerType, typesInjector)];
            })
            .OfType<(TServer Server, Type? HandlerType)>()
            .ToList();

        return CombineExecutions(
            configuredServers.ConvertAll(t =>
                RunServer(serverRunner, t.Server, t.HandlerType, serverFactory.TransportTypeName, cancellationToken)
            )
        );
    }

    public ReceiverExecutionHandle RunServer<THandler, TTypesInjector, TServer>(
        IIteratorServerFactory<TTypesInjector, TServer> serverFactory,
        IIteratorServerRunner<TServer> serverRunner,
        CancellationToken cancellationToken
    )
        where TTypesInjector : class, IIteratorHandlerTypesInjector
        where TServer : class
    {
        var registry = ServiceProvider.GetRequiredService<IIteratorHandlerRegistry>();
        var invokersForHandlerType = registry
            .GetServerHandlerInvokers<TTypesInjector>()
            .Where(i => i.HandlerType == typeof(THandler))
            .ToList();
        var typesInjector =
            invokersForHandlerType.FirstOrDefault()?.TypesInjector
            ?? throw new InvalidOperationException(
                $"did not find registrations for iterator handler type {typeof(THandler)}"
            );

        var server = CreateServerForHandlerType(
            serverFactory,
            typeof(THandler),
            invokersForHandlerType,
            typesInjector
        )?.Server;

        if (server is null)
        {
            return new ReceiverExecutionHandle(
                Task.CompletedTask,
                Task.CompletedTask,
                cancellationTokenSource: null,
                onDispose: null
            );
        }

        return RunServer(serverRunner, server, typeof(THandler), serverFactory.TransportTypeName, cancellationToken);
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
                // at this point all servers are running, so we can observe their completion tasks for any errors,
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

    private static (TServer Server, Type? HandlerType)? CreateServerForHandlerType<TTypesInjector, TServer>(
        IIteratorServerFactory<TTypesInjector, TServer> serverFactory,
        Type? handlerType,
        IReadOnlyCollection<IIteratorServerHandlerInvoker<TTypesInjector>> invokersForHandlerType,
        TTypesInjector typesInjector
    )
        where TTypesInjector : class, IIteratorHandlerTypesInjector
        where TServer : class
    {
        try
        {
            var server = serverFactory.CreateServerForHandlerType(handlerType, invokersForHandlerType, typesInjector);

            return server is null ? null : (server, handlerType);
        }
        catch (Exception ex)
        {
            throw new IteratorServerExecutionFailedException(
                $"failed to run the iterator server for handler type '{handlerType}'",
                ex
            )
            {
                HandlerType = handlerType,
                IteratorTransportType = new IteratorTransportType(
                    serverFactory.TransportTypeName,
                    IteratorTransportRole.Server
                ),
            };
        }
    }

    [SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "false positive")]
    private static ReceiverExecutionHandle RunServer<TServer>(
        IIteratorServerRunner<TServer> serverRunner,
        TServer server,
        Type? handlerType,
        string transportTypeName,
        CancellationToken cancellationToken
    )
        where TServer : class
    {
        try
        {
            return serverRunner.RunServer(server, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new IteratorServerExecutionFailedException(
                $"failed to run the iterator server for handler type '{handlerType}'",
                ex
            )
            {
                HandlerType = handlerType,
                IteratorTransportType = new IteratorTransportType(transportTypeName, IteratorTransportRole.Server),
            };
        }
    }
}
