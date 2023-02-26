using System.ComponentModel.DataAnnotations;

namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Contracts;

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery([Required] string CounterName);

public sealed record GetCounterValueQueryResponse(bool CounterExists, int? CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}
