using System.Threading;
using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.CQS
{
    public interface IQueryHandler
    {
    }

    public interface IQueryHandler<in TQuery, TResponse> : IQueryHandler
        where TQuery : class
    {
        Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken);
    }
}
