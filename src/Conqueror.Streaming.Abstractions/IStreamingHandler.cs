using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public interface IStreamingHandler
{
}

public interface IStreamingHandler<in TRequest, out TItem> : IStreamingHandler
    where TRequest : class
{
    IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken);
}
