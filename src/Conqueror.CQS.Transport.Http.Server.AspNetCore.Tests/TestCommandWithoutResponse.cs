namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
