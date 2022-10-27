using System;

namespace Conqueror
{
    public interface ICommandClientFactory
    {
        THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, ICommandTransportClient> transportBuilderFn, Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler;
    }
}
