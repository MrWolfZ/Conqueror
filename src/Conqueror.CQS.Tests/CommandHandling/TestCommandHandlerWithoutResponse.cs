using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithoutResponse : ITestCommandHandlerWithoutResponse
    {
        private readonly TestCommandResponses responses;

        private int invocationCount;

        public TestCommandHandlerWithoutResponse(TestCommandResponses responses)
        {
            this.responses = responses;
        }

        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            responses.Responses.Add(command.Payload + invocationCount);
        }
    }

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommand>
    {
    }
}
