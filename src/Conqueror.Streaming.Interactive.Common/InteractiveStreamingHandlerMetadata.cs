using System;

namespace Conqueror.Streaming.Interactive.Common;

public sealed class InteractiveStreamingHandlerMetadata
{
    public InteractiveStreamingHandlerMetadata(Type requestType, Type itemType, Type handlerType)
    {
        RequestType = requestType;
        ItemType = itemType;
        HandlerType = handlerType;
    }

    public Type RequestType { get; }

    public Type ItemType { get; }

    public Type HandlerType { get; }
}
