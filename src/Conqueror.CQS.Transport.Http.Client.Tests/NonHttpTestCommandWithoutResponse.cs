namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    public sealed record NonHttpTestCommandWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class NonHttpTestCommandWithoutResponseHandler : INonHttpTestCommandWithoutResponseHandler
    {
        public Task ExecuteCommand(NonHttpTestCommandWithoutResponse command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    public interface INonHttpTestCommandWithoutResponseHandler : ICommandHandler<NonHttpTestCommandWithoutResponse>
    {
    }
}
