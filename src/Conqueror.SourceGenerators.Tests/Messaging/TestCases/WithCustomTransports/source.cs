#nullable enable

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Messaging.WithCustomTransports.Transport1;
    using global::Messaging.WithCustomTransports.Transport2;

    [Message<TestMessageResponse>]
    [TestTransportMessage<TestMessageResponse>(StringProperty = "Test")]
    [TestTransport2Message<TestMessageResponse>(StringProperty = "Test2")]
    public partial record TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace Messaging.WithCustomTransports.Transport1
{
    using System;
    using Conqueror;
    using Conqueror.Messaging;

    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransports.Transport1")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportMessageAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransports.Transport1")]
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

namespace Messaging.WithCustomTransports.Transport2
{
    using System;
    using Conqueror;
    using Conqueror.Messaging;

    [MessageTransport(Prefix = "TestTransport2", Namespace = "Messaging.WithCustomTransports.Transport2")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransport2MessageAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    [MessageTransport(Prefix = "TestTransport2", Namespace = "Messaging.WithCustomTransports.Transport2")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransport2MessageAttribute<TResponse> : TestTransport2MessageAttribute;

    public interface ITestTransport2Message<TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransport2Message<TMessage, TResponse>
    {
        static virtual string? StringProperty { get; }
    }

    public interface ITestTransport2MessageHandler;

    public interface ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
        where TMessage : class, ITestTransport2Message<TMessage, TResponse>
        where TIHandler : class, ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
    {
        static IMessageHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
            where THandler : class, TIHandler => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
