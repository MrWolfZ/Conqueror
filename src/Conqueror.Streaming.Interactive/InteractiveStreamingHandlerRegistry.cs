using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Streaming.Interactive.Common;

namespace Conqueror.Streaming.Interactive
{
    internal sealed class InteractiveStreamingHandlerRegistry
    {
        private readonly IReadOnlyDictionary<(Type RequestType, Type ItemType), InteractiveStreamingHandlerMetadata> metadataLookup;

        public InteractiveStreamingHandlerRegistry(IEnumerable<InteractiveStreamingHandlerMetadata> metadata)
        {
            metadataLookup = metadata.ToDictionary(m => (m.RequestType, m.ItemType));
        }

        public InteractiveStreamingHandlerMetadata GetInteractiveStreamingHandlerMetadata<TRequest, TItem>()
            where TRequest : class
        {
            if (!metadataLookup.TryGetValue((typeof(TRequest), typeof(TItem)), out var metadata))
            {
                throw new ArgumentException($"there is no registered interactive streaming handler for request type '{typeof(TRequest).Name}' and item type '{typeof(TItem).Name}'");
            }

            return metadata;
        }
    }
}
