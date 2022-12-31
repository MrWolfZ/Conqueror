using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming.Interactive
{
    internal sealed class InteractiveStreamingHandlerProxy<TRequest, TItem> : IInteractiveStreamingHandler<TRequest, TItem>
        where TRequest : class
    {
        // TODO: private readonly InteractiveStreamingMiddlewaresInvoker invoker;
        private readonly InteractiveStreamingHandlerRegistry registry;
        private readonly IServiceProvider serviceProvider;

        public InteractiveStreamingHandlerProxy(InteractiveStreamingHandlerRegistry registry,
                                                //// TODO: InteractiveStreamingMiddlewaresInvoker invoker, 
                                                IServiceProvider serviceProvider)
        {
            this.registry = registry;
            //// this.invoker = invoker;
            this.serviceProvider = serviceProvider;
        }

        public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken)
        {
            var metadata = registry.GetInteractiveStreamingHandlerMetadata<TRequest, TItem>();
            var handler = serviceProvider.GetRequiredService(metadata.HandlerType) as IInteractiveStreamingHandler<TRequest, TItem>;
            return handler!.ExecuteRequest(request, cancellationToken);

            //// return invoker.InvokeMiddlewares<TRequest, TItem>(serviceProvider, metadata, request, cancellationToken);
        }
    }
}
