using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [HttpCommand]
    public sealed record TestCommand2;

    public sealed record TestCommandResponse2;

    public sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
    {
        public Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
