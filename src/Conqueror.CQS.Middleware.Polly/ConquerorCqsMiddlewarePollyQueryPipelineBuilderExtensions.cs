using Conqueror.CQS.Middleware.Polly;
using Polly;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror
{
    /// <summary>
    ///     Extension methods for <see cref="IQueryPipelineBuilder" /> to add, configure, or remove Polly functionality.
    /// </summary>
    public static class ConquerorCqsMiddlewarePollyQueryPipelineBuilderExtensions
    {
        /// <summary>
        ///     Wrap the execution of the rest of the query pipeline in a Polly <see cref="Polly.AsyncPolicy" />.
        /// </summary>
        /// <param name="pipeline">The query pipeline to add the Polly middleware to</param>
        /// <param name="policy">The policy to use to wrap the rest of the pipeline execution</param>
        /// <returns>The query pipeline</returns>
        public static IQueryPipelineBuilder UsePolly(this IQueryPipelineBuilder pipeline, AsyncPolicy policy)
        {
            return pipeline.Use<PollyQueryMiddleware, PollyQueryMiddlewareConfiguration>(new() { Policy = policy });
        }

        /// <summary>
        ///     Set the <see cref="Polly.AsyncPolicy" /> to use in the Polly middleware.
        /// </summary>
        /// <param name="pipeline">The query pipeline to configure the Polly middleware in</param>
        /// <param name="policy">The policy to use in the Polly middleware</param>
        /// <returns>The query pipeline</returns>
        public static IQueryPipelineBuilder ConfigurePollyPolicy(this IQueryPipelineBuilder pipeline, AsyncPolicy policy)
        {
            return pipeline.Configure<PollyQueryMiddleware, PollyQueryMiddlewareConfiguration>(o => o.Policy = policy);
        }

        /// <summary>
        ///     Remove the Polly middleware from a query pipeline.
        /// </summary>
        /// <param name="pipeline">The query pipeline with the Polly middleware to remove</param>
        /// <returns>The query pipeline</returns>
        public static IQueryPipelineBuilder WithoutPolly(this IQueryPipelineBuilder pipeline)
        {
            return pipeline.Without<PollyQueryMiddleware, PollyQueryMiddlewareConfiguration>();
        }
    }
}
