using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithMultipleMixedInterfaces : ICommandHandler<TestCommand>, ICommandHandler<TestCommand, TestCommandResponse>
    {
        Task ICommandHandler<TestCommand>.ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => ExecuteCommand(command, cancellationToken);

        public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
