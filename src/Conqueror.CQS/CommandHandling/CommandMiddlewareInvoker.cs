using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewareInvoker<TConfiguration> : ICommandMiddlewareInvoker
        where TConfiguration : CommandMiddlewareConfigurationAttribute
    {
        public async Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                                 CommandMiddlewareNext<TCommand, TResponse> next,
                                                                 CommandHandlerMetadata metadata,
                                                                 CommandMiddlewareConfigurationAttribute middlewareConfigurationAttribute,
                                                                 IServiceProvider serviceProvider,
                                                                 CancellationToken cancellationToken)
            where TCommand : class
        {
            var configurationAttribute = (TConfiguration)middlewareConfigurationAttribute;

            var ctx = new DefaultCommandMiddlewareContext<TCommand, TResponse, TConfiguration>(command, next, configurationAttribute, cancellationToken);

            var middleware = serviceProvider.GetRequiredService<CommandMiddlewareRegistry>().GetMiddleware<TConfiguration>(serviceProvider);

            return await middleware.Execute(ctx);
        }
    }
}
