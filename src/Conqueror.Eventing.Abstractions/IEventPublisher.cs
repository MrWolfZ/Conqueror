using System.Threading;
using System.Threading.Tasks;

namespace Conqueror
{
    public interface IEventPublisher
    {
        Task PublishEvent(object evt, CancellationToken cancellationToken);
    }
}
