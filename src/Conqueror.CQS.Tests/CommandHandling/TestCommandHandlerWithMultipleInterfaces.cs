using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithMultipleInterfaces : ICommandHandler<TestCommand, TestCommandResponse>, ICommandHandler<object, TestCommandResponse>
    {
        public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TestCommandResponse> ExecuteCommand(object command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
