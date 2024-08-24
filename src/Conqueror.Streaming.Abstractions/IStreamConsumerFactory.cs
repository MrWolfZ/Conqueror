using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IStreamConsumerFactory
{
    IStreamConsumer<TItem> Create<TItem>(Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn);

    IStreamConsumer<TItem> Create<TItem>(Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn,
                                         Action<IStreamConsumerPipelineBuilder> configurePipeline);
}
