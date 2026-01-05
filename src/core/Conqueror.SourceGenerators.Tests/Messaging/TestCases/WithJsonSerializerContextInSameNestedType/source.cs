namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInSameNestedType;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Message<TestMessageResponse>]
    public partial record TestMessage;

    public record TestMessageResponse;

    public partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    internal class TestMessageJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
    {
        public static JsonSerializerContext Default => null!;

        protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

        public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    public partial record TestMessage
    {
        public partial interface IHandler;
    }
}
