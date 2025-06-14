namespace Conqueror.Transport.ConformityTests.Signalling;

public interface ISignalTransportExecutionConformityTests<TTestHost, out TSuccessTestCase, out TErrorTestCase>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
    where TSuccessTestCase : ISignalTransportConformityExecutionSuccessTestCase<TTestHost>
    where TErrorTestCase : ISignalTransportConformityExecutionErrorTestCase<TTestHost>
{
    static abstract IEnumerable<TSuccessTestCase> CreateSuccessTestCases();

    static abstract IEnumerable<TSuccessTestCase> CreateSimpleSuccessTestCases();

    static abstract IEnumerable<TErrorTestCase> CreateErrorTestCases();

    static abstract IEnumerable<TErrorTestCase> CreateReconnectDelayTestCases();
}
