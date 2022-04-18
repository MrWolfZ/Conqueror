namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryMiddlewareThatShouldNeverBeCalledAttribute : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<TestQueryMiddlewareThatShouldNeverBeCalled>
    {
    }
}
