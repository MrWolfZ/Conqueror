using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Streaming;

internal sealed class StreamProducerRegistry : IStreamProducerRegistry
{
    private readonly IReadOnlyCollection<StreamProducerRegistration> registrations;

    public StreamProducerRegistry(IEnumerable<StreamProducerRegistration> registrations)
    {
        this.registrations = registrations.ToList();
    }

    public IReadOnlyCollection<StreamProducerRegistration> GetStreamProducerRegistrations() => registrations;
}
