using System.Threading;
using System.Threading.Tasks;

namespace Conqueror
{
    public interface ICommandTransport
    {
        Task<TResponse> Execute<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class;
    }
}
