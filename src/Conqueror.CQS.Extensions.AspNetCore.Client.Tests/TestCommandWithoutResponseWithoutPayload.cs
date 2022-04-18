using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
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
