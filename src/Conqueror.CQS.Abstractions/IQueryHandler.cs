using System.Threading;
using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror
{
    public interface IQueryHandler
    {
    }

    public interface IQueryHandler<in TQuery, TResponse> : IQueryHandler
        where TQuery : class
    {
        Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Note that this interface cannot be merged into <see cref="IQueryHandler"/> since it would
    /// disallow that interface to be used as generic parameter (see also this GitHub issue:
    /// https://github.com/dotnet/csharplang/issues/5955).
    /// </summary>
    public interface IConfigureQueryPipeline
    {
#if NET7_0_OR_GREATER
        static abstract void ConfigurePipeline(IQueryPipelineBuilder pipeline);
#endif
    }
}
