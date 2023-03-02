using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryTransportClient
{
    Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        where TQuery : class;
}
