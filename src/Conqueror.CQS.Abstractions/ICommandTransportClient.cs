using System.Threading;
using System.Threading.Tasks;

namespace Conqueror
{
    public interface ICommandTransportClient
    {
        Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class;
    }
}
