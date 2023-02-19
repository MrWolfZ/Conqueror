namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Contracts;

[HttpQuery]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}
