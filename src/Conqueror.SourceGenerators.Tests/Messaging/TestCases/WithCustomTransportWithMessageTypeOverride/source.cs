#nullable enable

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithMessageTypeOverride
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WithCustomTransportWithMessageTypeOverrideCustomTransport;

    [CustomTestTransportMessage<TestMessageResponse>(ExtraProperty = "Test")]
    public partial record TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace WithCustomTransportWithMessageTypeOverrideOriginalTransport
{
    using System;
    using Conqueror;
    using Conqueror.Messaging;

    [MessageTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportMessageAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [MessageTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

    public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    {
        static virtual string StringProperty => "Default";
    }

    public interface ITestTransportMessageHandler;

    public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
        where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    {
        static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler => throw new NotSupportedException();
    }
}

namespace WithCustomTransportWithMessageTypeOverrideCustomTransport
{
    using System;
    using Conqueror.Messaging;
    using WithCustomTransportWithMessageTypeOverrideOriginalTransport;

    [MessageTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport",
        FullyQualifiedMessageTypeName = "WithCustomTransportWithMessageTypeOverrideCustomTransport.ICustomTestTransportMessage"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomTestTransportMessageAttribute : Attribute
    {
        public string? ExtraProperty { get; init; }
    }

    [MessageTransport(
        Prefix = "TestTransport",
        Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport",
        FullyQualifiedMessageTypeName = "WithCustomTransportWithMessageTypeOverrideCustomTransport.ICustomTestTransportMessage"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomTestTransportMessageAttribute<TResponse> : CustomTestTransportMessageAttribute;

    public interface ICustomTestTransportMessage<TMessage, TResponse> : ITestTransportMessage<TMessage, TResponse>
        where TMessage : class, ICustomTestTransportMessage<TMessage, TResponse>
    {
        static virtual string? ExtraProperty { get; }

        static string ITestTransportMessage<TMessage, TResponse>.StringProperty { get; } =
            TMessage.ExtraProperty ?? "Default";
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithMessageTypeOverride
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
