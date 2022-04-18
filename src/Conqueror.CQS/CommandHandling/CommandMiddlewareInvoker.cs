using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandMiddlewareInvoker<TConfiguration> : ICommandMiddlewareInvoker
        where TConfiguration : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<ICommandMiddleware<TConfiguration>>
    {
        private static readonly Type MiddlewareType = typeof(TConfiguration).GetInterfaces()
                                                                            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddlewareConfiguration<>))
                                                                            .GetGenericArguments()
                                                                            .Single();

        public async Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                                 CommandMiddlewareNext<TCommand, TResponse> next,
                                                                 CommandHandlerMetadata metadata,
                                                                 IServiceProvider serviceProvider,
                                                                 CancellationToken cancellationToken)
            where TCommand : class
        {
            if (!metadata.TryGetMiddlewareConfigurationAttribute<TConfiguration>(out var configurationAttribute))
            {
                return await next(command, cancellationToken);
            }

            var ctx = new DefaultCommandMiddlewareContext<TCommand, TResponse, TConfiguration>(command, next, configurationAttribute, cancellationToken);

            var middleware = serviceProvider.GetService(MiddlewareType);

            if (middleware is null)
            {
                throw new InvalidOperationException($"command middleware ${MiddlewareType.Name} is not registered, but is being used on handler ${metadata.HandlerType.Name}");
            }

            return await ((ICommandMiddleware<TConfiguration>)middleware).Execute(ctx);
        }
    }
}
