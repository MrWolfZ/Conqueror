#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Signalling;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithMixedTransports;

[SignalTransport(Prefix = "TestTransport", Namespace = "Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithMixedTransports")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[SignalTransport(Prefix = "TestTransport2", Namespace = "Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithMixedTransports")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2SignalAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    static virtual string StringProperty => "Default";
}

public interface ITestTransport2Signal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransport2Signal<TSignal>
{
    static virtual string? StringProperty { get; }
}

public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotSupportedException();
}

public interface ITestTransport2SignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransport2Signal<TSignal>
    where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotSupportedException();
}

[TestTransportSignal(StringProperty = "Test")]
[TestTransport2Signal(StringProperty = "Test2")]
public partial record TestSignal;

[TestTransport2Signal(StringProperty = "Test2")]
public partial record TestSignal2;

public partial class TestSignalHandler : TestSignal.IHandler,
                                         TestSignal2.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task Handle(TestSignal2 message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}

public partial record TestSignal2
{
    public partial interface IHandler;
}
