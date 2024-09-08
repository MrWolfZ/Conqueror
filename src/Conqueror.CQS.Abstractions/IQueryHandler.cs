using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types

namespace Conqueror;

public interface IQueryHandler
{
}

public interface IQueryHandler<TQuery, TResponse> : IQueryHandler
    where TQuery : class
{
    Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(IQueryPipeline<TQuery, TResponse> pipeline)
    {
        // by default, we use an empty pipeline
    }
}
