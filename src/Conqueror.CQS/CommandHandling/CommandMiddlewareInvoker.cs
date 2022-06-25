using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

// these classes belong together
#pragma warning disable SA1402

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewareInvoker<TConfiguration> : ICommandMiddlewareInvoker
    {
        public async Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                                 CommandMiddlewareNext<TCommand, TResponse> next,
                                                                 Type middlewareType,
                                                                 object? middlewareConfiguration,
                                                                 IServiceProvider serviceProvider,
                                                                 CancellationToken cancellationToken)
            where TCommand : class
        {
            if (middlewareConfiguration is null)
            {
                throw new ArgumentNullException(nameof(middlewareConfiguration));
            }

            var configuration = (TConfiguration)middlewareConfiguration;

            var ctx = new DefaultCommandMiddlewareContext<TCommand, TResponse, TConfiguration>(command, next, configuration, cancellationToken);

            var middleware = (ICommandMiddleware<TConfiguration>)serviceProvider.GetRequiredService(middlewareType);

            return await middleware.Execute(ctx);
        }
    }
    
    internal sealed class CommandMiddlewareInvoker : ICommandMiddlewareInvoker
    {
        public async Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                                 CommandMiddlewareNext<TCommand, TResponse> next,
                                                                 Type middlewareType,
                                                                 object? middlewareConfiguration,
                                                                 IServiceProvider serviceProvider,
                                                                 CancellationToken cancellationToken)
            where TCommand : class
        {
            var ctx = new DefaultCommandMiddlewareContext<TCommand, TResponse>(command, next, cancellationToken);

            var middleware = (ICommandMiddleware)serviceProvider.GetRequiredService(middlewareType);

            return await middleware.Execute(ctx);
        }
    }
}
