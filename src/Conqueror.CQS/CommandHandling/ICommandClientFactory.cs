using System;

namespace Conqueror.CQS.CommandHandling
{
    public interface ICommandClientFactory
    {
        THandler CreateCommandClient<THandler>(Func<ICommandTransportBuilder, ICommandTransport> transportBuilderFn, Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler;
    }
}
