using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.ResponseInDifferentNamespace
{
    using Some.Other.Namespace;

    [Message<TestMessageResponse>]
    public partial record TestMessage;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace Some.Other.Namespace
{
    public record TestMessageResponse;
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.ResponseInDifferentNamespace
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
