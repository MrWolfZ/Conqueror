namespace Conqueror.Recipes.CQS.Advanced.ExposingViaHttp;

public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>;

internal sealed class GetCounterValueQueryHandler(CountersRepository repository) : IGetCounterValueQueryHandler
{
    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
