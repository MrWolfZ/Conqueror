using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalHandler
{
    static virtual void ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
    {
        // we don't configure the receiver (by default, it is disabled for all signal types)
    }
}

public interface IHttpSseSignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>,
                                                             IHttpSseSignalHandler
    where TSignal : class, IHttpSseSignal<TSignal>
    where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateHttpSseTypesInjector<THandler>()
        where THandler : class, TIHandler
        => HttpSseSignalHandlerTypesInjector<TSignal, TIHandler, THandler>.Default;
}
