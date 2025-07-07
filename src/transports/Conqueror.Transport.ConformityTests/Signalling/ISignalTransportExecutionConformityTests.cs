namespace Conqueror.Transport.ConformityTests.Signalling;

public interface ISignalTransportExecutionConformityTests<TTestHost, out TSuccessTestCase, out TErrorTestCase>
    where TTestHost : ISignalTransportConformityTestHost
    where TSuccessTestCase : ISignalTransportConformityExecutionSuccessTestCase<TTestHost>
    where TErrorTestCase : ISignalTransportConformityExecutionErrorTestCase<TTestHost>
{
    static abstract bool TransportBuffersSignalsDuringReceiverDowntime { get; }

    static abstract bool TransportSupportsReconnectingReceivers { get; }

    static abstract bool TransportUsesCompetingConsumers { get; }

    static abstract IEnumerable<TSuccessTestCase> CreateSuccessTestCases();

    static abstract IEnumerable<TSuccessTestCase> CreateSimpleSuccessTestCases();

    static abstract IEnumerable<TErrorTestCase> CreateErrorTestCases();
}
