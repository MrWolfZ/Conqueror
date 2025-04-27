using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.InSameNestedTypeProtectedWithoutResponse;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Message]
    protected partial record TestMessage;

    protected partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    protected partial record TestMessage
    {
        public partial interface IHandler;
    }
}
