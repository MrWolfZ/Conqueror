using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandler : ITestCommandHandler
    {
        private int invocationCount;

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new() { Payload = command.Payload + invocationCount };
        }
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }
}
