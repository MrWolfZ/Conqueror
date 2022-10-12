using System.Threading;
using System.Threading.Tasks;

// false positive warning about static member on generic interface, since member is abstract
#pragma warning disable CA1000

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.CQS
{
    public interface ICommandHandler
    {
    }

    public interface ICommandHandler<in TCommand> : ICommandHandler
        where TCommand : class
    {
        Task ExecuteCommand(TCommand command, CancellationToken cancellationToken);
    }

    public interface ICommandHandler<in TCommand, TResponse> : ICommandHandler
        where TCommand : class
    {
        Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken);
    }
    
    public interface IConfigureCommandHandlerPipeline
    {
#if NET7_0_OR_GREATER
        static abstract void ConfigurePipeline(ICommandPipelineBuilder pipeline);
#endif
    }
}
