using Polly;
using PollyPolicy = Polly.Policy;

namespace Conqueror.CQS.Middleware.Polly
{
    /// <summary>
    ///     The configuration options for <see cref="PollyQueryMiddleware" />.
    /// </summary>
    public sealed class PollyQueryMiddlewareConfiguration
    {
        /// <summary>
        ///     The policy to use to wrap the rest of the pipeline execution.
        /// </summary>
        public AsyncPolicy Policy { get; set; } = PollyPolicy.NoOpAsync();
    }
}
