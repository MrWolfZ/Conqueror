using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithEnumerableResponseArrayOfPrimitive;

[Message<int[]>]
public partial record TestMessage;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<int[]> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}
