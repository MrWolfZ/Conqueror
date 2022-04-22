using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class DefaultCommandMiddlewareContext<TCommand, TResponse, TConfiguration> : CommandMiddlewareContext<TCommand, TResponse, TConfiguration>
        where TCommand : class
        where TConfiguration : CommandMiddlewareConfigurationAttribute
    {
        private readonly CommandMiddlewareNext<TCommand, TResponse> next;

        public DefaultCommandMiddlewareContext(TCommand command, CommandMiddlewareNext<TCommand, TResponse> next, TConfiguration configuration, CancellationToken cancellationToken)
        {
            this.next = next;
            Command = command;
            CancellationToken = cancellationToken;
            Configuration = configuration;
        }

        public override TCommand Command { get; }

        public override bool HasUnitResponse { get; } = typeof(TResponse) == typeof(UnitCommandResponse);

        public override CancellationToken CancellationToken { get; }

        public override TConfiguration Configuration { get; }

        public override Task<TResponse> Next(TCommand command, CancellationToken cancellationToken) => next(command, cancellationToken);
    }
}
