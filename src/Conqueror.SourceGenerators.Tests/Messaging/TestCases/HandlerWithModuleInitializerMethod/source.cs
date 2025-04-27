using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.HandlerWithModuleInitializerMethod;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();

    [ModuleInitializer]
    public static void ModuleInitializer()
    {
    }
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}

public partial class TestMessageHandler : IMessageHandler;
