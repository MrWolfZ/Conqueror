namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class TestCommandMiddlewareThatShouldNeverBeCalledAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<TestCommandMiddlewareThatShouldNeverBeCalled>
    {
    }
}
