using Conqueror.Signalling;

namespace Conqueror.Middleware.Logging.Tests.Signalling;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name",
                 Justification = "we want to bundle all files for the transport here, so the file name makes sense")]
[SignalTransport(Prefix = "TestTransport", Namespace = "Conqueror.Middleware.Logging.Tests.Signalling")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute;

public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>;

public interface ITestTransportSignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler>.Default;
}

internal interface ITestTransportSignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    TResult Create<TResult>(ITestTransportSignalHandlerTypesInjectable<TResult> injectable);
}

file sealed class TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler> : ITestTransportSignalHandlerTypesInjector
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
    where THandler : class, TIHandler
{
    public static readonly TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler> Default = new();

    public Type SignalType { get; } = typeof(TSignal);

    public TResult Create<TResult>(ITestTransportSignalHandlerTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TSignal, TIHandler, THandler>();
}

public interface ITestTransportSignalHandlerTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler, THandler>()
        where TSignal : class, ITestTransportSignal<TSignal>
        where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler;
}
