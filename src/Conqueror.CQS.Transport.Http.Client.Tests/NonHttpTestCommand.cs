namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    public sealed record NonHttpTestCommand
    {
        public int Payload { get; init; }
    }

    public sealed class NonHttpTestCommandHandler : INonHttpTestCommandHandler
    {
        public Task<TestCommandResponse> ExecuteCommand(NonHttpTestCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public interface INonHttpTestCommandHandler : ICommandHandler<NonHttpTestCommand, TestCommandResponse>
    {
    }
}
