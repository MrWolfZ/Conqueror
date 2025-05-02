using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithMultiplePartials;

[Message<TestMessageResponse>]
public partial record TestMessage;

public partial record TestMessage : IMessage<TestMessage, TestMessageResponse>;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;

    public static ICoreMessageHandlerTypesInjector CoreTypesInjector => null!;

    public static TestMessage? EmptyInstance => null;

    public static System.Collections.Generic.IEnumerable<System.Reflection.ConstructorInfo> PublicConstructors => null!;

    public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> PublicProperties => null!;
}
