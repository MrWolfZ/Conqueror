using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public interface IInteractiveStreamingHandler
{
}

public interface IInteractiveStreamingHandler<in TRequest, out TItem> : IInteractiveStreamingHandler
    where TRequest : class
{
    IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken);
}
