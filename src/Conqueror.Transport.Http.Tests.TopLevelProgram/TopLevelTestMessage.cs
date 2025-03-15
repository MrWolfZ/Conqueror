namespace Conqueror.Transport.Http.Tests.TopLevelProgram;

public sealed partial record TopLevelTestMessage(int Payload) : IMessage<TopLevelTestMessageResponse>;

public sealed record TopLevelTestMessageResponse(int Payload);

internal sealed class TopLevelTestMessageHandler : TopLevelTestMessage.IHandler
{
    public async Task<TopLevelTestMessageResponse> Handle(TopLevelTestMessage command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Payload + 1);
    }
}
