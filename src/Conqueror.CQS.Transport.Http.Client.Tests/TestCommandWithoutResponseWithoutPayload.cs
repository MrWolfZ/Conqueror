namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutResponseWithoutPayload;

    public sealed class TestCommandWithoutResponseWithoutPayloadHandler : ITestCommandWithoutResponseWithoutPayloadHandler
    {
        public async Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public interface ITestCommandWithoutResponseWithoutPayloadHandler : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
    {
    }
}
