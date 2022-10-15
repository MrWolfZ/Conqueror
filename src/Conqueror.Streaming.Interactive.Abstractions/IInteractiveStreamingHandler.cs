using System.Collections.Generic;
using System.Threading;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror
{
    public interface IInteractiveStreamingHandler
    {
    }

    public interface IInteractiveStreamingHandler<in TRequest, out TItem> : IInteractiveStreamingHandler
        where TRequest : class
    {
        IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken);
    }
}
