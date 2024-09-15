using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Streaming;

internal sealed class StreamProducerRegistry(IEnumerable<StreamProducerRegistration> registrations) : IStreamProducerRegistry
{
    private readonly IReadOnlyCollection<StreamProducerRegistration> registrations = registrations.ToList();

    public IReadOnlyCollection<StreamProducerRegistration> GetStreamProducerRegistrations() => registrations;
}
