using System;

namespace Conqueror;

public interface ICommandPipeline<TCommand> : ICommandPipeline<TCommand, UnitCommandResponse>
    where TCommand : class;

public interface ICommandPipeline<TCommand, TResponse>
    where TCommand : class
{
    IServiceProvider ServiceProvider { get; }

    ICommandPipeline<TCommand, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>;

    ICommandPipeline<TCommand, TResponse> Without<TMiddleware>()
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>;

    ICommandPipeline<TCommand, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ICommandMiddleware<TCommand, TResponse>;
}
