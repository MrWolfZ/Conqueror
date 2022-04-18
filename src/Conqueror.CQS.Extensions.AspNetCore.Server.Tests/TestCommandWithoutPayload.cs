using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutPayload;

    public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = 11 };
        }
    }
}
