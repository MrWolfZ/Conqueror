using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInDifferentNamespace;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInDifferentNamespace
{
    [Message<TestMessageResponse>]
    public sealed partial record TestMessage;

    public sealed record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace Some.Other.NamespaceWithJsonSerializerContext
{
    [JsonSerializable(typeof(TestMessage))]
    [JsonSerializable(typeof(TestMessageResponse))]
    internal class TestMessageJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
    {
        protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

        public static JsonSerializerContext Default => null!;

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
