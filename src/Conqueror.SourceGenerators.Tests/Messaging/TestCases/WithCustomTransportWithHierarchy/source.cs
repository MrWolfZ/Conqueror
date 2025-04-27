#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Messaging;
using Messaging.WithCustomTransportWithHierarchy;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithHierarchy
{
    [TestTransportMessage<TestMessageResponse>]
    public abstract partial record TestMessage;

    [TestTransportMessage<TestMessageResponse>]
    public partial record TestMessageSub : TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public partial class TestMessageSubHandler : TestMessageSub.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessageSub message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace Messaging.WithCustomTransportWithHierarchy
{
    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportMessageAttribute : Attribute;

    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

    public interface ITestTransportMessage<out TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>;

    public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
        where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    {
        static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler
            => throw new NotSupportedException();
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
