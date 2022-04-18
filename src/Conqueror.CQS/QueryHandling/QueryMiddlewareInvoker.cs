using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewareInvoker<TConfiguration> : IQueryMiddlewareInvoker
        where TConfiguration : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<IQueryMiddleware<TConfiguration>>
    {
        private static readonly Type MiddlewareType = typeof(TConfiguration).GetInterfaces()
                                                                            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddlewareConfiguration<>))
                                                                            .GetGenericArguments()
                                                                            .Single();

        public async Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                               QueryMiddlewareNext<TQuery, TResponse> next,
                                                               QueryHandlerMetadata metadata,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TQuery : class
        {
            if (!metadata.TryGetMiddlewareConfiguration<TConfiguration>(out var configurationAttribute))
            {
                return await next(query, cancellationToken);
            }

            var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration>(query, next, configurationAttribute, cancellationToken);

            var middleware = serviceProvider.GetService(MiddlewareType);

            if (middleware is null)
            {
                throw new InvalidOperationException($"query middleware ${MiddlewareType.Name} is not registered, but is being used on handler ${metadata.HandlerType.Name}");
            }

            return await ((IQueryMiddleware<TConfiguration>)middleware).Execute(ctx);
        }
    }
}
