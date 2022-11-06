namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestCommandWithoutResponseHandler : ITestCommandWithoutResponseHandler
    {
        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
    }
}
