using System;

namespace Conqueror
{
    public interface IQueryClientFactory
    {
        THandler CreateQueryClient<THandler>(Func<IQueryTransportClientBuilder, IQueryTransportClient> transportBuilderFn, Action<IQueryPipelineBuilder>? configurePipeline = null)
            where THandler : class, IQueryHandler;
    }
}
