using System;
using System.Collections.Generic;
using System.Linq;
using Conqueror.Streaming.Common;

namespace Conqueror.Streaming;

internal sealed class StreamingHandlerRegistry
{
    private readonly IReadOnlyDictionary<(Type RequestType, Type ItemType), StreamingHandlerMetadata> metadataLookup;

    public StreamingHandlerRegistry(IEnumerable<StreamingHandlerMetadata> metadata)
    {
        metadataLookup = metadata.ToDictionary(m => (m.RequestType, m.ItemType));
    }

    public StreamingHandlerMetadata GetStreamingHandlerMetadata<TRequest, TItem>()
        where TRequest : class
    {
        if (!metadataLookup.TryGetValue((typeof(TRequest), typeof(TItem)), out var metadata))
        {
            throw new ArgumentException($"there is no registered streaming handler for request type '{typeof(TRequest).Name}' and item type '{typeof(TItem).Name}'");
        }

        return metadata;
    }
}
