namespace Conqueror.Transport.ConformityTests.Signalling;

public interface ISignalTransportConformityTestHost<TTestHost> : ITransportConformityTestHost
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
{
    ISignalReceivers SignalReceivers { get; }

    ISignalPublishers SignalPublishers { get; }

    IConquerorContextAccessor PublisherConquerorContextAccessor { get; }
}
