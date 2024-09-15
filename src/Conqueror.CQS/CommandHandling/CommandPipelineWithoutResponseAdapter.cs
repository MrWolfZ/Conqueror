using System;
using System.Collections;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipelineWithoutResponseAdapter<TCommand>(
    ICommandPipeline<TCommand, UnitCommandResponse> commandPipelineImplementation)
    : ICommandPipeline<TCommand>
    where TCommand : class
{
    public IServiceProvider ServiceProvider => commandPipelineImplementation.ServiceProvider;

    public ConquerorContext ConquerorContext => commandPipelineImplementation.ConquerorContext;

    public CommandTransportType TransportType => commandPipelineImplementation.TransportType;

    public int Count => commandPipelineImplementation.Count;

    public ICommandPipeline<TCommand, UnitCommandResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ICommandMiddleware<TCommand, UnitCommandResponse>
    {
        return commandPipelineImplementation.Use(middleware);
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Use(CommandMiddlewareFn<TCommand, UnitCommandResponse> middlewareFn)
    {
        return commandPipelineImplementation.Use(middlewareFn);
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware<TCommand, UnitCommandResponse>
    {
        return commandPipelineImplementation.Without<TMiddleware>();
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ICommandMiddleware<TCommand, UnitCommandResponse>
    {
        return commandPipelineImplementation.Configure(configure);
    }

    public IEnumerator<ICommandMiddleware<TCommand, UnitCommandResponse>> GetEnumerator() => commandPipelineImplementation.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
