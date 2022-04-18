using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithoutResponseWithMiddleware : ICommandHandler<TestCommand>
    {
        private readonly TestCommandResponses responses;

        private int invocationCount;

        public TestCommandHandlerWithoutResponseWithMiddleware(TestCommandResponses responses)
        {
            this.responses = responses;
        }

        [TestCommandMiddleware(Increment = 2)]
        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            responses.Responses.Add(command.Payload + invocationCount);
        }
    }
}
