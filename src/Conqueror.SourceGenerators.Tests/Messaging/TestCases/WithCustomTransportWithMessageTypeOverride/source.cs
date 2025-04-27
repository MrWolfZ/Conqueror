#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Messaging;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithMessageTypeOverride
{
    using WithCustomTransportWithMessageTypeOverrideCustomTransport;

    [CustomTestTransportMessage<TestMessageResponse>(ExtraProperty = "Test")]
    public partial record TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace WithCustomTransportWithMessageTypeOverrideOriginalTransport
{
    [MessageTransport(Prefix = "TestTransport", Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportMessageAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [MessageTransport(Prefix = "TestTransport", Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

    public interface ITestTransportMessage<out TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    {
        static virtual string StringProperty => "Default";
    }

    public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
        where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    {
        static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler
            => throw new NotSupportedException();
    }
}

namespace WithCustomTransportWithMessageTypeOverrideCustomTransport
{
    [MessageTransport(Prefix = "TestTransport", Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport",
    FullyQualifiedMessageTypeName = "WithCustomTransportWithMessageTypeOverrideCustomTransport.ICustomTestTransportMessage")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomTestTransportMessageAttribute : Attribute
    {
        public string? ExtraProperty { get; init; }
    }

    [MessageTransport(Prefix = "TestTransport", Namespace = "WithCustomTransportWithMessageTypeOverrideOriginalTransport",
                      FullyQualifiedMessageTypeName = "WithCustomTransportWithMessageTypeOverrideCustomTransport.ICustomTestTransportMessage")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomTestTransportMessageAttribute<TResponse> : CustomTestTransportMessageAttribute;

    public interface ICustomTestTransportMessage<out TMessage, TResponse> : WithCustomTransportWithMessageTypeOverrideOriginalTransport.ITestTransportMessage<TMessage, TResponse>
        where TMessage : class, ICustomTestTransportMessage<TMessage, TResponse>
    {
        static string WithCustomTransportWithMessageTypeOverrideOriginalTransport.ITestTransportMessage<TMessage, TResponse>.StringProperty { get; } = TMessage.ExtraProperty ?? "Default";

        static virtual string? ExtraProperty { get; }
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
