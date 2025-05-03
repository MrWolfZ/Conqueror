using System.Text.Json.Serialization;

namespace Conqueror.Transport.Http.Tests.AOT.TopLevelProgram;

[HttpMessage<TopLevelTestMessageResponse>(
    PathPrefix = "api/prefix",
    Version = "v1",
    Path = "messages/test",
    ApiGroupName = "Test Messages",
    Name = "test-message-name")]
public sealed partial record TopLevelTestMessage
{
    public required int Payload { get; init; }

    public required NestedObject Nested { get; init; }
}

public sealed record NestedObject
{
    public required string NestedString { get; init; }
}

public sealed record TopLevelTestMessageResponse(int Payload);

internal sealed partial class TopLevelTestMessageHandler : TopLevelTestMessage.IHandler
{
    public async Task<TopLevelTestMessageResponse> Handle(TopLevelTestMessage message, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(message.Payload + 1);
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TopLevelTestMessage))]
[JsonSerializable(typeof(TopLevelTestMessageResponse))]
internal sealed partial class TopLevelTestMessageJsonSerializerContext : JsonSerializerContext;
