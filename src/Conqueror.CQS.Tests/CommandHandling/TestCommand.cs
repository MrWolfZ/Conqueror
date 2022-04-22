namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed record TestCommand
    {
        public int Payload { get; init; }
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }
}
