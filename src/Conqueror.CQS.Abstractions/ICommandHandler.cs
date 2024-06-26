using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : class
{
    Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse> : ICommandHandler
    where TCommand : class
{
    Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
///     Note that this interface cannot be merged into <see cref="ICommandHandler" /> since it would
///     disallow that interface to be used as generic parameter (see also this GitHub issue:
///     https://github.com/dotnet/csharplang/issues/5955).
/// </summary>
public interface IConfigureCommandPipeline
{
    static abstract void ConfigurePipeline(ICommandPipelineBuilder pipeline);
}
