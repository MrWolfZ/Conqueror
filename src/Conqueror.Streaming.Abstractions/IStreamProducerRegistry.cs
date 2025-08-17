namespace Conqueror;

public interface IStreamProducerRegistry
{
    IReadOnlyCollection<StreamProducerRegistration> GetStreamProducerRegistrations();
}

public sealed record StreamProducerRegistration(Type RequestType, Type ItemType, Type ProducerType);
