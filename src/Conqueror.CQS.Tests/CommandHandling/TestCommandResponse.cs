namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed record TestCommandResponse
    {
        public int Payload { get; init; }
    }
}
