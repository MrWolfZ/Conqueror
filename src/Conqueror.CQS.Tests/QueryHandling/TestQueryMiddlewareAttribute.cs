namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class TestQueryMiddlewareAttribute : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<TestQueryMiddleware>
    {
        public int Increment { get; set; }
    }
}
