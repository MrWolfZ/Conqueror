using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [HttpCommand]
    public sealed record TestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
