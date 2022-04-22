using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.CQS
{
    public interface ICommandMiddleware
    {
    }

    public interface ICommandMiddleware<TConfiguration> : ICommandMiddleware
        where TConfiguration : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<ICommandMiddleware<TConfiguration>>
    {
        Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TConfiguration> ctx)
            where TCommand : class;
    }
}
