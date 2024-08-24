using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

#pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types

public interface IStreamConsumer;

public interface IStreamConsumer<in TItem> : IStreamConsumer
{
    Task HandleItem(TItem item, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
    {
        // by default, we use an empty pipeline
    }
}
