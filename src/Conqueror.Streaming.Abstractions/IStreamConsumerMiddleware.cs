using System.Threading.Tasks;

namespace Conqueror;

public interface IStreamConsumerMiddleware : IStreamConsumerMiddlewareMarker
{
    Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx);
}

public interface IStreamConsumerMiddleware<TConfiguration> : IStreamConsumerMiddlewareMarker
{
    Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TConfiguration> ctx);
}

public interface IStreamConsumerMiddlewareMarker;
