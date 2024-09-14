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
    private readonly Action<IQueryPipeline<TQuery, TResponse>>? configurePipeline;
    private readonly IServiceProvider serviceProvider;
    private readonly QueryTransportClientFactory transportClientFactory;

    public QueryHandlerProxy(IServiceProvider serviceProvider,
                             QueryTransportClientFactory transportClientFactory,
                             Action<IQueryPipeline<TQuery, TResponse>>? configurePipeline)
    {
        this.serviceProvider = serviceProvider;
        this.transportClientFactory = transportClientFactory;
        this.configurePipeline = configurePipeline;
    }

    public async Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var transportTypeName = conquerorContext.GetExecutionTransportTypeName();
        if (transportTypeName is null || conquerorContext.GetQueryId() is null)
        {
            conquerorContext.SetQueryId(ActivitySpanId.CreateRandom().ToString());
        }

        var pipelineBuilder = new QueryPipeline<TQuery, TResponse>(serviceProvider);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return await pipeline.Execute(serviceProvider,
                                      query,
                                      transportClientFactory,
                                      transportTypeName,
                                      cancellationToken)
                             .ConfigureAwait(false);
    }

    public IQueryHandler<TQuery, TResponse> WithPipeline(Action<IQueryPipeline<TQuery, TResponse>> configure)
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
                                                        configure);
    }
}
