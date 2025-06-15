namespace Conqueror.Transport.ConformityTests.Messaging;

public interface IMessageTransportContextConformityTests<TTestHost, out TTestCase>
    where TTestHost : IMessageTransportConformityTestHost
    where TTestCase : IMessageTransportConformityContextTestCase<TTestHost>
{
    static abstract IEnumerable<TTestCase> CreateTestCases();
}
