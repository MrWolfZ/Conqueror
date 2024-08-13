using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IStreamingRequestHandlerRegistry
{
    public IReadOnlyCollection<StreamingRequestHandlerRegistration> GetStreamingRequestHandlerRegistrations();
}

public sealed record StreamingRequestHandlerRegistration(Type StreamingRequestType, Type ItemType, Type HandlerType);
