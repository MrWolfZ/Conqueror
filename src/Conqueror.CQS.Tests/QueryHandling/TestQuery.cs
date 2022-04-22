namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed record TestQuery
    {
        public int Payload { get; init; }
    }
    
    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }
}
