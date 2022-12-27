namespace Conqueror.Examples.BlazorWebAssembly.Contracts;

[HttpQuery(Version = 1, ApiGroupName = "SharedCounter")]
public sealed record GetSharedCounterValueQuery;

public sealed record GetSharedCounterValueQueryResponse(long Value);

public interface IGetSharedCounterValueQueryHandler : IQueryHandler<GetSharedCounterValueQuery, GetSharedCounterValueQueryResponse>
{
}
