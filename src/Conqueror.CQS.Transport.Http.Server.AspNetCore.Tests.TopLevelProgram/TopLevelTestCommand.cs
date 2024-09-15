namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests.TopLevelProgram;

[HttpCommand]
public sealed record TopLevelTestCommand(int Payload);

public sealed record TopLevelTestCommandResponse(int Payload);

public interface ITopLevelTestCommandHandler : ICommandHandler<TopLevelTestCommand, TopLevelTestCommandResponse>;

internal sealed class TopLevelTestCommandHandler : ITopLevelTestCommandHandler
{
    public async Task<TopLevelTestCommandResponse> Handle(TopLevelTestCommand command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Payload + 1);
    }
}
