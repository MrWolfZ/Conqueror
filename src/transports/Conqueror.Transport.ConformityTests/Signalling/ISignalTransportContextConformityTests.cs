namespace Conqueror.Transport.ConformityTests.Signalling;

public interface ISignalTransportContextConformityTests<TTestHost, out TTestCase>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
    where TTestCase : ISignalTransportConformityContextTestCase<TTestHost>
{
    static abstract IEnumerable<TTestCase> CreateTestCases();
}
