namespace Conqueror.Recipes.CQS.Advanced.TestingHttp;

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>;

internal sealed class GetCounterValueQueryHandler(CountersRepository repository) : IGetCounterValueQueryHandler
{
    public async Task<GetCounterValueQueryResponse> Handle(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
