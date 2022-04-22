using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMiddlewareRegistry
    {
        private readonly IReadOnlyDictionary<Type, EventObserverMiddlewareMetadata> metadataLookup;

        public EventObserverMiddlewareRegistry(IEnumerable<EventObserverMiddlewareMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => m.AttributeType);
        }

        public IEventObserverMiddleware<TConfiguration> GetMiddleware<TConfiguration>(IServiceProvider serviceProvider)
            where TConfiguration : EventObserverMiddlewareConfigurationAttribute
        {
            if (!metadataLookup.TryGetValue(typeof(TConfiguration), out var metadata))
            {
                throw new ArgumentException($"there is no registered event observer middleware for attribute {typeof(TConfiguration).Name}");
            }

            return (IEventObserverMiddleware<TConfiguration>)serviceProvider.GetRequiredService(metadata.MiddlewareType);
        }
    }
}
