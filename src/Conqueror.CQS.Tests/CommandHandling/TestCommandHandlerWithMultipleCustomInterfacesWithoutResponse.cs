using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse : ITestCommandHandlerWithoutResponse, ITestCommandHandlerWithoutResponse2
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

    public interface ITestCommandHandlerWithoutResponse2 : ICommandHandler<object>
    {
    }
}
