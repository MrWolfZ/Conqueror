using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types

namespace Conqueror;

public interface ICommandHandler
{
}

public interface ICommandHandler<TCommand> : ICommandHandler
    where TCommand : class
{
    Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(ICommandPipeline<TCommand> pipeline)
    {
        // by default, we use an empty pipeline
    }
}

public interface ICommandHandler<TCommand, TResponse> : ICommandHandler
    where TCommand : class
{
    Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(ICommandPipeline<TCommand, TResponse> pipeline)
    {
        // by default, we use an empty pipeline
    }
}
