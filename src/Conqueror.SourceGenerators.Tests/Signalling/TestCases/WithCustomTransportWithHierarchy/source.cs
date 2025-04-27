#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Signalling;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithHierarchy;

[SignalTransport(Prefix = "TestTransport", Namespace = "Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithHierarchy")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute;

public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>;

public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotSupportedException();
}

[TestTransportSignal]
public abstract partial record TestSignal(int Payload);

[TestTransportSignal]
public sealed partial record TestSignalSub(int Payload) : TestSignal(Payload);

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestSignalSubHandler : TestSignalSub.IHandler
{
    public Task Handle(TestSignalSub message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}

public partial record TestSignalSub
{
    public new partial interface IHandler;
}
