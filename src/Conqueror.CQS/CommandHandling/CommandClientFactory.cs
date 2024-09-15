using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror.Common;

namespace Conqueror.CQS.CommandHandling;

// TODO: improve performance by caching creation functions via compiled expressions
internal sealed class CommandClientFactory
{
    public THandler CreateCommandClient<THandler>(IServiceProvider serviceProvider,
                                                  Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory)
        where THandler : class, ICommandHandler
    {
        typeof(THandler).ValidateNoInvalidCommandHandlerInterface();

        if (!typeof(THandler).IsInterface)
        {
            throw new ArgumentException($"can only create command client for command handler interfaces, got concrete type '{typeof(THandler).Name}'");
        }

        var commandAndResponseTypes = typeof(THandler).GetCommandAndResponseTypes();

        switch (commandAndResponseTypes.Count)
        {
            case < 1:
                throw new ArgumentException($"type {typeof(THandler).Name} does not implement any command handler interface");

            case > 1:
                throw new ArgumentException($"type {typeof(THandler).Name} implements multiple command handler interfaces");
        }

        var (commandType, responseType) = commandAndResponseTypes.First();

        var creationMethod = typeof(CommandClientFactory).GetMethod(nameof(CreateCommandClientInternal), BindingFlags.NonPublic | BindingFlags.Static);

        if (creationMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(CreateCommandClientInternal)}'");
        }

        var genericCreationMethod = creationMethod.MakeGenericMethod(typeof(THandler), commandType, responseType ?? typeof(UnitCommandResponse));

        try
        {
            var result = genericCreationMethod.Invoke(null, [serviceProvider, transportClientFactory]);

            if (result is not THandler handler)
            {
                throw new InvalidOperationException($"failed to create command client for handler type '{typeof(THandler).Name}'");
            }

            return handler;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable code that is necessary so that the compiler knows the catch throws
        }
    }

    private static THandler CreateCommandClientInternal<THandler, TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                                       Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory)
        where THandler : class, ICommandHandler
        where TCommand : class
    {
        var proxy = new CommandHandlerProxy<TCommand, TResponse>(serviceProvider, new(transportClientFactory), null, CommandTransportRole.Client);

        if (typeof(THandler) == typeof(ICommandHandler<TCommand>))
        {
            return (THandler)(object)new CommandHandlerWithoutResponseAdapter<TCommand>((ICommandHandler<TCommand, UnitCommandResponse>)(object)proxy);
        }

        if (typeof(THandler) == typeof(ICommandHandler<TCommand, TResponse>))
        {
            return (THandler)(object)proxy;
        }

        if (typeof(THandler).IsAssignableTo(typeof(ICommandHandler<TCommand>)))
        {
            var proxyType = ProxyTypeGenerator.Create(typeof(THandler), typeof(ICommandHandler<TCommand>), typeof(CommandHandlerGeneratedProxyBase<TCommand>));
            var adapter = new CommandHandlerWithoutResponseAdapter<TCommand>((ICommandHandler<TCommand, UnitCommandResponse>)(object)proxy);
            return (THandler)Activator.CreateInstance(proxyType, adapter)!;
        }

        if (typeof(THandler).IsAssignableTo(typeof(ICommandHandler<TCommand, TResponse>)))
        {
            var proxyType = ProxyTypeGenerator.Create(typeof(THandler), typeof(ICommandHandler<TCommand, TResponse>), typeof(CommandHandlerGeneratedProxyBase<TCommand, TResponse>));
            return (THandler)Activator.CreateInstance(proxyType, proxy)!;
        }

        throw new InvalidOperationException($"command handler type '{typeof(THandler).Name}' does not implement a known command handler interface");
    }
}
