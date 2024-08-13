using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public interface IStreamingRequestHandler;

public interface IStreamingRequestHandler<in TRequest, out TItem> : IStreamingRequestHandler
    where TRequest : class
{
    IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
///     Note that this interface cannot be merged into <see cref="IStreamingRequestHandler" /> since
///     it would disallow that interface to be used as generic parameter (see also this GitHub issue:
///     https://github.com/dotnet/csharplang/issues/5955).
/// </summary>
public interface IConfigureStreamingRequestPipeline
{
    static abstract void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline);
}
