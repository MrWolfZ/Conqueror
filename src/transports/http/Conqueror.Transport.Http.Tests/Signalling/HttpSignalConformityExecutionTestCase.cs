namespace Conqueror.Transport.Http.Tests.Signalling;

public abstract class HttpSignalConformityExecutionTestCase : HttpSignalConformityTestCase,
                                                              ISignalTransportConformityExecutionTestCase<HttpSignalTransportConformityTestHost>
{
    public int NumOfReceivers { get; init; } = 1;

    public required IReadOnlyCollection<object> ExpectedReceivedSignals { get; init; }
}
