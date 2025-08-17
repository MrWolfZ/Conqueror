namespace Conqueror.Transport.Http.Tests.TopLevelProgram;

[HttpMessage<TopLevelTestMessageResponse>(Path = "test")]
internal sealed partial record TopLevelTestMessage
{
    public required int Payload { get; init; }

    public required NestedObject Nested { get; init; }
}

internal sealed record NestedObject
{
    public required string NestedString { get; init; }
}

internal sealed record TopLevelTestMessageResponse(int Payload);

internal sealed partial class TopLevelTestMessageHandler : TopLevelTestMessage.IHandler
{
    public async Task<TopLevelTestMessageResponse> Handle(
        TopLevelTestMessage message,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Yield();

        return new TopLevelTestMessageResponse(message.Payload + 1);
    }
}
