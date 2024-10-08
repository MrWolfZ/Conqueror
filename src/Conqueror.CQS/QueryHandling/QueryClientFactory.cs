using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror.Common;

namespace Conqueror.CQS.QueryHandling;

// TODO: improve performance by caching creation functions via compiled expressions
internal sealed class QueryClientFactory
{
    public THandler CreateQueryClient<THandler>(IServiceProvider serviceProvider,
                                                Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        typeof(THandler).ValidateNoInvalidQueryHandlerInterface();

        if (!typeof(THandler).IsInterface)
        {
            throw new ArgumentException($"can only create query client for query handler interfaces, got concrete type '{typeof(THandler).Name}'");
        }

        var queryAndResponseTypes = typeof(THandler).GetQueryAndResponseTypes();

        switch (queryAndResponseTypes.Count)
        {
            case < 1:
                throw new ArgumentException($"type {typeof(THandler).Name} does not implement any query handler interface");

            case > 1:
                throw new ArgumentException($"type {typeof(THandler).Name} implements multiple query handler interfaces");
        }

        var (queryType, responseType) = queryAndResponseTypes.First();

        var creationMethod = typeof(QueryClientFactory).GetMethod(nameof(CreateQueryClientInternal), BindingFlags.NonPublic | BindingFlags.Static);

        if (creationMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(CreateQueryClientInternal)}'");
        }

        var genericCreationMethod = creationMethod.MakeGenericMethod(typeof(THandler), queryType, responseType);

        try
        {
            var result = genericCreationMethod.Invoke(null, [serviceProvider, transportClientFactory]);

            if (result is not THandler handler)
            {
                throw new InvalidOperationException($"failed to create query client for handler type '{typeof(THandler).Name}'");
            }

            return handler;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable code that is necessary so that the compiler knows the catch throws
        }
    }

    private static THandler CreateQueryClientInternal<THandler, TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                                   Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler
        where TQuery : class
    {
        var proxy = new QueryHandlerProxy<TQuery, TResponse>(serviceProvider, new(transportClientFactory), null, QueryTransportRole.Client);

        if (typeof(THandler) == typeof(IQueryHandler<TQuery, TResponse>))
        {
            return (THandler)(object)proxy;
        }

        if (typeof(THandler).IsAssignableTo(typeof(IQueryHandler<TQuery, TResponse>)))
        {
            var proxyType = ProxyTypeGenerator.Create(typeof(THandler), typeof(IQueryHandler<TQuery, TResponse>), typeof(QueryHandlerGeneratedProxyBase<TQuery, TResponse>));
            return (THandler)Activator.CreateInstance(proxyType, proxy)!;
        }

        throw new InvalidOperationException($"query handler type '{typeof(THandler).Name}' does not implement a known query handler interface");
    }
}
