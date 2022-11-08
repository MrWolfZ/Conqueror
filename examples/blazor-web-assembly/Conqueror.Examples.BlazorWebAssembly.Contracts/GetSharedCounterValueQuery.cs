namespace Conqueror.Examples.BlazorWebAssembly.Contracts;

[HttpQuery]
public sealed record GetSharedCounterValueQuery;

public sealed record GetSharedCounterValueQueryResponse(long Value);

public interface IGetSharedCounterValueQueryHandler : IQueryHandler<GetSharedCounterValueQuery, GetSharedCounterValueQueryResponse>
{
}