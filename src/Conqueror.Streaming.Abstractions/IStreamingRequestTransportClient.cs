using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public interface IStreamingRequestTransportClient
{
    IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                            IServiceProvider serviceProvider,
                                                            CancellationToken cancellationToken)
        where TRequest : class;
}
