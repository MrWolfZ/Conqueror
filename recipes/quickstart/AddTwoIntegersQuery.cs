using Conqueror;

namespace Quickstart;

[HttpQuery]
public sealed record AddTwoIntegersQuery(int Parameter1, int Parameter2);

public sealed record AddTwoIntegersQueryResponse(int Sum);

public interface IAddTwoIntegersQueryHandler : IQueryHandler<AddTwoIntegersQuery, AddTwoIntegersQueryResponse>
{
}

public sealed class AddTwoIntegersQueryHandler : IAddTwoIntegersQueryHandler
{
    public Task<AddTwoIntegersQueryResponse> ExecuteQuery(AddTwoIntegersQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AddTwoIntegersQueryResponse(query.Parameter1 + query.Parameter2));
    }
}
