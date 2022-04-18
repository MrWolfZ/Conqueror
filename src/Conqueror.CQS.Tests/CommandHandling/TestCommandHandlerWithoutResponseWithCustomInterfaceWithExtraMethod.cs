using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod : ITestCommandHandlerWithoutResponseWithExtraMethod
    {
        public Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public void ExtraMethod()
        {
            throw new NotSupportedException();
        }
    }

    public interface ITestCommandHandlerWithoutResponseWithExtraMethod : ICommandHandler<TestCommand>
    {
        void ExtraMethod();
    }
}
