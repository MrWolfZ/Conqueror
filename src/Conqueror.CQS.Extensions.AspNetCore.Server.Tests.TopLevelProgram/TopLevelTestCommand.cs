namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests.TopLevelProgram;

[HttpCommand]
public sealed record TopLevelTestCommand(int Payload);

public sealed record TopLevelTestCommandResponse(int Payload);

public interface ITopLevelTestCommandHandler : ICommandHandler<TopLevelTestCommand, TopLevelTestCommandResponse>
{
}

internal sealed class TopLevelTestCommandHandler : ITopLevelTestCommandHandler
{
    public async Task<TopLevelTestCommandResponse> ExecuteCommand(TopLevelTestCommand command, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new(command.Payload + 1);
    }
}
