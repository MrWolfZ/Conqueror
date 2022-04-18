// generic interface parameter is used

#pragma warning disable CA1040

namespace Conqueror.Eventing
{
    // ReSharper disable once UnusedTypeParameter
    public interface IEventObserverMiddlewareConfiguration<out TMiddleware>
    {
    }
}
