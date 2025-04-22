using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalMiddleware<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    Task Execute(SignalMiddlewareContext<TSignal> ctx);
}
