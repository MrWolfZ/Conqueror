namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutPayload;

    public sealed class TestCommandWithoutPayloadHandler : ITestCommandWithoutPayloadHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }

    public interface ITestCommandWithoutPayloadHandler : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
    }
}
