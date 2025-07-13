namespace Conqueror.Transport.ConformityTests.Messaging;

public interface IMessageTransportExecutionConformityTests<TTestHost, out TSuccessTestCase, out TErrorTestCase>
    where TTestHost : IMessageTransportConformityTestHost
    where TSuccessTestCase : IMessageTransportConformityExecutionSuccessTestCase<TTestHost>
    where TErrorTestCase : IMessageTransportConformityExecutionErrorTestCase<TTestHost>
{
    // for transports that support e.g. competing consumers for a queue
    static abstract bool TransportSupportsConcurrentReceivers { get; }

    static abstract bool TransportBuffersMessagesDuringReceiverDowntime { get; }

    static abstract bool TransportRequiresReceiverConnection { get; }

    static abstract string TransportTypeName { get; }

    static abstract IEnumerable<TSuccessTestCase> CreateSuccessTestCases();

    static abstract IEnumerable<TSuccessTestCase> CreateSimpleSuccessTestCases();

    static abstract IEnumerable<TErrorTestCase> CreateErrorTestCases();
}
