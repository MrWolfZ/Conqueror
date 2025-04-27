using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.InSameNestedType;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Message<TestMessageResponse>]
    public partial record TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
