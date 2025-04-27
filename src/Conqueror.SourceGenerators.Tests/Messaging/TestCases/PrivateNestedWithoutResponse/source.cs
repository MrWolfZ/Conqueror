using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.PrivateNestedWithoutResponse;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Message]
    private partial record TestMessage;

    private partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    private partial record TestMessage
    {
        public partial interface IHandler;
    }
}
