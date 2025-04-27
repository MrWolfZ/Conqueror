using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithMultiplePartialsWithoutResponse;

[Message]
public partial record TestMessage;

public partial record TestMessage : IMessage<TestMessage, UnitMessageResponse>;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;

    public static TestMessage? EmptyInstance => null;

    public static System.Collections.Generic.IEnumerable<System.Reflection.ConstructorInfo> PublicConstructors => null!;

    public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> PublicProperties => null!;
}
