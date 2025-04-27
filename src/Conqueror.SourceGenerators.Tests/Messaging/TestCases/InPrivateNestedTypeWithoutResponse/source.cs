using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.InPrivateNestedTypeWithoutResponse;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    private sealed partial class PrivateClass
    {
        [Message]
        public partial record TestMessage;

        public partial class TestMessageHandler : TestMessage.IHandler
        {
            public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}

// make the compiler happy during design time
public partial class Container
{
    private sealed partial class PrivateClass
    {
        public partial record TestMessage
        {
            public partial interface IHandler;
        }
    }
}
