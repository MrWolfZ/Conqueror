using System;
using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryClientFactory
{
    THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory, Action<IQueryPipelineBuilder>? configurePipeline = null)
        where THandler : class, IQueryHandler
    {
        return CreateQueryClient<THandler>(b => Task.FromResult(transportClientFactory(b)), configurePipeline);
    }

    THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory, Action<IQueryPipelineBuilder>? configurePipeline = null)
        where THandler : class, IQueryHandler;
}
