using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    public sealed record NonHttpTestCommand
    {
        public int Payload { get; init; }
    }

    public sealed class NonHttpTestCommandHandler : INonHttpTestCommandHandler
    {
        public Task<TestCommandResponse> ExecuteCommand(NonHttpTestCommand command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    public interface INonHttpTestCommandHandler : ICommandHandler<NonHttpTestCommand, TestCommandResponse>
    {
    }
}
