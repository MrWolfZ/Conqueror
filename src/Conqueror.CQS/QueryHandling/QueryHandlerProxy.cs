using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryHandlerProxy<TQuery, TResponse>(
    IServiceProvider serviceProvider,
    QueryTransportClientFactory transportClientFactory,
    Action<IQueryPipeline<TQuery, TResponse>>? configurePipeline,
    QueryTransportRole transportRole)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : class
{
    public async Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var transportTypeName = conquerorContext.GetExecutionTransportTypeName();
        var isInProcessClient = transportTypeName is null && transportRole == QueryTransportRole.Client;
        if (conquerorContext.GetQueryId() is null || isInProcessClient)
        {
            conquerorContext.SetQueryId(ActivitySpanId.CreateRandom().ToString());
        }

        var transportClient = await transportClientFactory.Create(typeof(TQuery), typeof(TResponse), serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new QueryTransportType(transportTypeName ?? transportClient.TransportTypeName, transportRole);

        var pipelineBuilder = new QueryPipeline<TQuery, TResponse>(serviceProvider, conquerorContext, transportType);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return await pipeline.Execute(serviceProvider,
                                      query,
                                      transportClient,
                                      transportType,
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
                                                        configure,
                                                        transportRole);
    }
}
