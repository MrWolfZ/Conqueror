using System;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandPipelineWithoutResponseAdapter<TCommand> : ICommandPipeline<TCommand>
    where TCommand : class
{
    private readonly ICommandPipeline<TCommand, UnitCommandResponse> commandPipelineImplementation;

    public CommandPipelineWithoutResponseAdapter(ICommandPipeline<TCommand, UnitCommandResponse> commandPipelineImplementation)
    {
        this.commandPipelineImplementation = commandPipelineImplementation;
    }

    public IServiceProvider ServiceProvider => commandPipelineImplementation.ServiceProvider;

    public ICommandPipeline<TCommand, UnitCommandResponse> Use<TMiddleware>()
        where TMiddleware : ICommandMiddleware
    {
        return commandPipelineImplementation.Use<TMiddleware>();
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : ICommandMiddleware<TConfiguration>
    {
        return commandPipelineImplementation.Use<TMiddleware, TConfiguration>(configuration);
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware
    {
        return commandPipelineImplementation.Without<TMiddleware>();
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Without<TMiddleware, TConfiguration>()
        where TMiddleware : ICommandMiddleware<TConfiguration>
    {
        return commandPipelineImplementation.Without<TMiddleware, TConfiguration>();
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : ICommandMiddleware<TConfiguration>
    {
        return commandPipelineImplementation.Configure<TMiddleware, TConfiguration>(configuration);
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : ICommandMiddleware<TConfiguration>
    {
        return commandPipelineImplementation.Configure<TMiddleware, TConfiguration>(configure);
    }

    public ICommandPipeline<TCommand, UnitCommandResponse> Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : ICommandMiddleware<TConfiguration>
    {
        return commandPipelineImplementation.Configure<TMiddleware, TConfiguration>(configure);
    }
}
