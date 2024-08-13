using System;

namespace Conqueror.Streaming.Common;

public sealed class StreamingHandlerMetadata
{
    public StreamingHandlerMetadata(Type requestType, Type itemType, Type handlerType)
    {
        RequestType = requestType;
        ItemType = itemType;
        HandlerType = handlerType;
    }

    public Type RequestType { get; }

    public Type ItemType { get; }

    public Type HandlerType { get; }
}
