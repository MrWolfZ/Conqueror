using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IStreamProducerRegistry
{
    public IReadOnlyCollection<StreamProducerRegistration> GetStreamProducerRegistrations();
}

public sealed record StreamProducerRegistration(Type RequestType, Type ItemType, Type ProducerType);
