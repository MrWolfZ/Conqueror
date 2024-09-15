namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

// this interface can be resolved or injected to call a handler; note that the interface
// must not have any extra methods, it just inherits from the generic handler interface
public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>;

internal sealed class GetCounterValueQueryHandler(CountersRepository repository) : IGetCounterValueQueryHandler
{
    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        return new(await repository.GetCounterValue(query.CounterName));
    }
}
