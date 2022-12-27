namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [HttpCommand]
    public sealed record TestCommand2;

    public sealed record TestCommandResponse2;

    public sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
    {
        public Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
