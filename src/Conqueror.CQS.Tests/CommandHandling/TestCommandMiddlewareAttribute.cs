namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandMiddlewareAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<TestCommandMiddleware>
    {
        public int Increment { get; set; }
    }
}
