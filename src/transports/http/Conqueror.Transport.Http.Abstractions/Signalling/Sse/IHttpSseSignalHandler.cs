using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpSseSignalHandler
{
    static abstract void ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver);
}

public interface IHttpSseSignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>
    where TSignal : class, IHttpSseSignal<TSignal>
    where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateHttpSseTypesInjector<THandler>()
        where THandler : class, TIHandler, IHttpSseSignalHandler
        => new HttpSseSignalHandlerTypesInjector<TSignal, TIHandler>(THandler.ConfigureHttpSseReceiver);
}
