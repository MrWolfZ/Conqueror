using System;
using System.Threading.Tasks;

namespace Conqueror
{
    public interface ICommandClientFactory
    {
        THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory, Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler
        {
            return CreateCommandClient<THandler>(b => Task.FromResult(transportClientFactory(b)), configurePipeline);
        }

        THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory, Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler;
    }
}
