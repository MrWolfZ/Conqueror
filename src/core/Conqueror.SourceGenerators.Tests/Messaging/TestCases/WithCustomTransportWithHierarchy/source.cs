#nullable enable

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithHierarchy
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Messaging.WithCustomTransportWithHierarchy;

    [TestTransportMessage<TestMessageResponse>]
    public abstract partial record TestMessage;

    [TestTransportMessage<TestMessageResponse>]
    public partial record TestMessageSub : TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    public partial class TestMessageSubHandler : TestMessageSub.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessageSub message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace Messaging.WithCustomTransportWithHierarchy
{
    using System;
    using Conqueror;
    using Conqueror.Messaging;

    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportMessageAttribute : Attribute;

    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

    public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>;

    public interface ITestTransportMessageHandler;

    public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
        where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    {
        static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithHierarchy
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }

    public partial record TestMessageSub
    {
        public new partial interface IHandler;
    }
}
