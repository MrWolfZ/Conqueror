using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror.Common;
using Conqueror.CQS.Common;

namespace Conqueror.CQS.CommandHandling;

// TODO: improve performance by caching creation functions via compiled expressions
internal sealed class CommandClientFactory
{
    public THandler CreateCommandClient<THandler>(IServiceProvider serviceProvider,
                                                  Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory,
                                                  Action<ICommandPipelineBuilder>? configurePipeline)
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
            var result = genericCreationMethod.Invoke(null, new object?[] { serviceProvider, transportClientFactory, configurePipeline });

            if (result is not THandler handler)
            {
                throw new InvalidOperationException($"failed to create command client for handler type '{typeof(THandler).Name}'");
            }

            return handler;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static THandler CreateCommandClientInternal<THandler, TCommand, TResponse>(IServiceProvider serviceProvider,
                                                                                       Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory,
                                                                                       Action<ICommandPipelineBuilder>? configurePipeline)
        where THandler : class, ICommandHandler
        where TCommand : class
    {
        var proxy = new CommandHandlerProxy<TCommand, TResponse>(serviceProvider, transportClientFactory, configurePipeline);

        if (typeof(THandler) == typeof(ICommandHandler<TCommand>))
        {
            return (THandler)(object)new CommandWithoutResponseAdapter<TCommand>((ICommandHandler<TCommand, UnitCommandResponse>)(object)proxy);
        }

        if (typeof(THandler) == typeof(ICommandHandler<TCommand, TResponse>))
        {
            return (THandler)(object)proxy;
        }

        if (typeof(THandler).IsAssignableTo(typeof(ICommandHandler<TCommand>)))
        {
            var dynamicType = DynamicType.Create(typeof(THandler), typeof(ICommandHandler<TCommand>));
            var adapter = new CommandWithoutResponseAdapter<TCommand>((ICommandHandler<TCommand, UnitCommandResponse>)(object)proxy);
            return (THandler)Activator.CreateInstance(dynamicType, adapter)!;
        }

        if (typeof(THandler).IsAssignableTo(typeof(ICommandHandler<TCommand, TResponse>)))
        {
            var dynamicType = DynamicType.Create(typeof(THandler), typeof(ICommandHandler<TCommand, TResponse>));
            return (THandler)Activator.CreateInstance(dynamicType, proxy)!;
        }

        throw new InvalidOperationException($"command handler type '{typeof(THandler).Name}' does not implement a known command handler interface");
    }
}
