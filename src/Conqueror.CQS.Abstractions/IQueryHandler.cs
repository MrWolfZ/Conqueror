using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryHandler
{
}

public interface IQueryHandler<in TQuery, TResponse> : IQueryHandler
    where TQuery : class
{
    Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
///     Note that this interface cannot be merged into <see cref="IQueryHandler" /> since it would
///     disallow that interface to be used as generic parameter (see also this GitHub issue:
///     https://github.com/dotnet/csharplang/issues/5955).
/// </summary>
public interface IConfigureQueryPipeline
{
        static abstract void ConfigurePipeline(IQueryPipelineBuilder pipeline);
}
