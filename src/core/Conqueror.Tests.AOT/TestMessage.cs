namespace Conqueror.Tests.AOT;

using System.Text.Json.Serialization;

[Message<TestMessageResponse>]
internal sealed partial record TestMessage
{
    public required int Payload { get; init; }
}

internal sealed record TestMessageResponse(int Payload);

internal sealed partial class TestMessageHandler(ISignalPublishers signalPublishers) : TestMessage.IHandler
{
    public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
    {
        await signalPublishers.For(TestSignal.T).Handle(new() { Payload = message.Payload }, cancellationToken);

        return new TestMessageResponse(message.Payload + 1);
    }

    public static void ConfigurePipeline(TestMessage.IPipeline pipeline) { }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TestMessage))]
[JsonSerializable(typeof(TestMessageResponse))]
internal sealed partial class TestMessageJsonSerializerContext : JsonSerializerContext;
