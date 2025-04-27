using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithJsonSerializerContextInSameNamespaceWithoutResponse;

[Message]
public sealed partial record TestMessage;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

[JsonSerializable(typeof(TestMessage))]
internal class TestMessageJsonSerializerContext(JsonSerializerOptions options) : JsonSerializerContext(options)
{
    public override JsonTypeInfo GetTypeInfo(Type type) => throw new NotSupportedException();

    protected override JsonSerializerOptions GeneratedSerializerOptions => null!;

    public static JsonSerializerContext Default => null!;
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}
