using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryHandlerProxy<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : class
{
    private readonly Action<IQueryPipelineBuilder>? configurePipeline;
    private readonly QueryMiddlewareRegistry queryMiddlewareRegistry;
    private readonly IServiceProvider serviceProvider;
    private readonly QueryTransportClientFactory transportClientFactory;

    public QueryHandlerProxy(IServiceProvider serviceProvider,
                             QueryTransportClientFactory transportClientFactory,
                             Action<IQueryPipelineBuilder>? configurePipeline,
                             QueryMiddlewareRegistry queryMiddlewareRegistry)
    {
        this.serviceProvider = serviceProvider;
        this.transportClientFactory = transportClientFactory;
        this.configurePipeline = configurePipeline;
        this.queryMiddlewareRegistry = queryMiddlewareRegistry;
    }

    public async Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        if (!conquerorContext.IsExecutionFromTransport() || conquerorContext.GetQueryId() is null)
        {
            conquerorContext.SetQueryId(ActivitySpanId.CreateRandom().ToString());
        }

        var pipelineBuilder = new QueryPipelineBuilder(serviceProvider, queryMiddlewareRegistry);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return await pipeline.Execute<TQuery, TResponse>(serviceProvider, query, transportClientFactory, cancellationToken).ConfigureAwait(false);
    }

    public IQueryHandler<TQuery, TResponse> WithPipeline(Action<IQueryPipelineBuilder> configure)
    {
        if (configurePipeline is not null)
        {
            var originalConfigure = configure;
            configure = pipeline =>
            {
                originalConfigure(pipeline);
                configurePipeline(pipeline);
            };
        }

        return new QueryHandlerProxy<TQuery, TResponse>(serviceProvider,
                                                        transportClientFactory,
                                                        configure,
                                                        queryMiddlewareRegistry);
    }
}
