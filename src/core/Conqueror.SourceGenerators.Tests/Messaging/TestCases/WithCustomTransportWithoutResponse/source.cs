#nullable enable

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithoutResponse
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Messaging.WithCustomTransportWithoutResponse;

    [TestTransportMessage(StringProperty = "Test", IntProperty = 1, IntArrayProperty = [1, 2, 3], NullProperty = null)]
    public partial record TestMessage;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task Handle(TestMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace Messaging.WithCustomTransportWithoutResponse
{
    using System;
    using Conqueror;
    using Conqueror.Messaging;

    [MessageTransport(Prefix = "TestTransport", Namespace = "Messaging.WithCustomTransportWithoutResponse")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TestTransportMessageAttribute : Attribute
    {
        public string? StringProperty { get; init; }

        public int IntProperty { get; init; }

        public int[]? IntArrayProperty { get; init; }

        public string? NullProperty { get; init; }

        public string? UnsetProperty { get; init; }
    }

    [MessageTransport(
        Prefix = "TestTransport",
        Namespace = "Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithoutResponse"
    )]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

    public interface ITestTransportMessage<TMessage, TResponse> : IMessage<TMessage, TResponse>
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    {
        static virtual string StringProperty => "Default";

        static virtual int IntProperty { get; }

        static virtual int[] IntArrayProperty { get; } = [];

        static virtual string? NullProperty { get; }

        static virtual string? UnsetProperty { get; }
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

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransportWithoutResponse
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
