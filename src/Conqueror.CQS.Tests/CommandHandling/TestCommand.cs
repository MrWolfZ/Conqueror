namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed record TestCommand
    {
        public int Payload { get; init; }
    }
}
