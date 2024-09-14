using System;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipelineWithoutResponseAdapter<TCommand>(
    ICommandPipeline<TCommand, UnitCommandResponse> commandPipelineImplementation) : ICommandPipeline<TCommand>
    where TCommand : class
{
    public IServiceProvider ServiceProvider => commandPipelineImplementation.ServiceProvider;

    public ICommandPipeline<TCommand, UnitCommandResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ICommandMiddleware
    {
        return commandPipelineImplementation.Use(middleware);
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware
    {
        return commandPipelineImplementation.Without<TMiddleware>();
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ICommandMiddleware
    {
        return commandPipelineImplementation.Configure(configure);
    }
}
