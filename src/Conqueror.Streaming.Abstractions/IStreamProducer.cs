using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public interface IStreamProducer;

public interface IStreamProducer<in TRequest, out TItem> : IStreamProducer
    where TRequest : class
{
    IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
///     Note that this interface cannot be merged into <see cref="IStreamProducer" /> since
///     it would disallow that interface to be used as generic parameter (see also this GitHub issue:
///     https://github.com/dotnet/csharplang/issues/5955).
/// </summary>
public interface IConfigureStreamProducerPipeline
{
    static abstract void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline);
}
