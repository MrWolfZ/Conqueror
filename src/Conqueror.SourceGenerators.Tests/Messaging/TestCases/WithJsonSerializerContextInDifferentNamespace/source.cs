namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInDifferentNamespace
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage;

    public sealed record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace Some.Other.NamespaceWithJsonSerializerContext
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInDifferentNamespace;

    [JsonSerializable(typeof(TestMessage))]
    [JsonSerializable(typeof(TestMessageResponse))]
    internal class TestMessageJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
    {
        public static JsonSerializerContext Default => null!;

        protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

        public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInDifferentNamespace
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
