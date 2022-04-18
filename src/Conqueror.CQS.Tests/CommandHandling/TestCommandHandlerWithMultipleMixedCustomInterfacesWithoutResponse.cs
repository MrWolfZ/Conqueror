using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse : ITestCommandHandlerWithoutResponse, ICommandHandler<object>
    {
        public Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ExecuteCommand(object command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
