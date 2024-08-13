using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestHandlerRegistry : IStreamingRequestHandlerRegistry
{
    private readonly IReadOnlyCollection<StreamingRequestHandlerRegistration> registrations;

    public StreamingRequestHandlerRegistry(IEnumerable<StreamingRequestHandlerRegistration> registrations)
    {
        this.registrations = registrations.ToList();
    }

    public IReadOnlyCollection<StreamingRequestHandlerRegistration> GetStreamingRequestHandlerRegistrations() => registrations;
}
