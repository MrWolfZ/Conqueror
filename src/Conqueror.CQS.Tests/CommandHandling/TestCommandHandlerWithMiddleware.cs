using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithMiddleware : ICommandHandler<TestCommand, TestCommandResponse>
    {
        [TestCommandMiddleware(Increment = 2)]
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new() { Payload = command.Payload + 1 };
        }
    }
}
