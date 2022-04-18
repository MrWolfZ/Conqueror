using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
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
