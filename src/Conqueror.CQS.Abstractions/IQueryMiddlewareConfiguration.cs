// generic interface parameter is used
#pragma warning disable CA1040

namespace Conqueror.CQS
{
    // ReSharper disable once UnusedTypeParameter
    public interface IQueryMiddlewareConfiguration<out TMiddleware>
    {
    }
}
