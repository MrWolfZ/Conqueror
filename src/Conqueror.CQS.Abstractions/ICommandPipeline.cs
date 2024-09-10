using System;

namespace Conqueror;

public interface ICommandPipeline<TCommand> : ICommandPipeline<TCommand, UnitCommandResponse>
    where TCommand : class;

public interface ICommandPipeline<TCommand, TResponse>
    where TCommand : class
{
    IServiceProvider ServiceProvider { get; }

    ICommandPipeline<TCommand, TResponse> Use<TMiddleware>()
        where TMiddleware : ICommandMiddleware;

    ICommandPipeline<TCommand, TResponse> Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : ICommandMiddleware<TConfiguration>;

    ICommandPipeline<TCommand, TResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware;

    ICommandPipeline<TCommand, TResponse> Without<TMiddleware, TConfiguration>()
        where TMiddleware : ICommandMiddleware<TConfiguration>;

    ICommandPipeline<TCommand, TResponse> Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : ICommandMiddleware<TConfiguration>;

    ICommandPipeline<TCommand, TResponse> Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : ICommandMiddleware<TConfiguration>;

    ICommandPipeline<TCommand, TResponse> Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : ICommandMiddleware<TConfiguration>;
}
