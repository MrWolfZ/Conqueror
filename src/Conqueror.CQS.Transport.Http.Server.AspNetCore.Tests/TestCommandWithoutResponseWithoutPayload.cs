namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutResponseWithoutPayload;

    public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
    {
        public async Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
