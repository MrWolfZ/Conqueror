using System;
using System.Linq;
using System.Reflection;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client
{
    internal sealed class HttpClientFactory
    {
        private readonly ConfigurationProvider configurationProvider;

        public HttpClientFactory(ConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public TStreamingHandler CreateInteractiveStreamingHttpClient<TStreamingHandler>(IServiceProvider serviceProvider,
                                                                                         Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                         Action<ConquerorInteractiveStreamingHttpClientOptions>? configureOptions = null)
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            var (requestType, itemType) = typeof(TStreamingHandler).GetInteractiveStreamingRequestAndItemTypes().Single();
            requestType.AssertRequestIsHttpInteractiveStream();

            var registration = new HttpClientRegistration(typeof(TStreamingHandler), baseAddressFactory)
            {
                ConfigurationAction = configureOptions,
            };

            var method = typeof(HttpClientFactory).GetMethod(nameof(CreateTypedQueryClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
            var typedMethod = method?.MakeGenericMethod(typeof(TStreamingHandler), requestType, itemType);
            var result = typedMethod?.Invoke(this, new object?[] { serviceProvider, registration });
            return result as TStreamingHandler ?? throw new InvalidOperationException($"could not created typed query client for handler type {typeof(TStreamingHandler).Name}");
        }

        private object? CreateTypedQueryClientGeneric<TStreamingHandler, TRequest, TItem>(IServiceProvider serviceProvider, HttpClientRegistration registration)
            where TStreamingHandler : class, IInteractiveStreamingHandler
            where TRequest : class
        {
            var handler = new HttpInteractiveStreamingHandler<TRequest, TItem>(configurationProvider.GetOptions(serviceProvider, registration),
                                                                               serviceProvider.GetService<IConquerorContextAccessor>());

            if (!typeof(TStreamingHandler).IsCustomInteractiveStreamingHandlerInterfaceType())
            {
                return handler;
            }

            var dynamicType = DynamicType.Create(typeof(TStreamingHandler), typeof(IInteractiveStreamingHandler<TRequest, TItem>));
            return Activator.CreateInstance(dynamicType, handler);
        }
    }
}
