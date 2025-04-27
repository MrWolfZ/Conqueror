#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Signalling;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransport;

[SignalTransport(Prefix = "TestTransport", Namespace = "Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransport")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute
{
    public string? StringProperty { get; init; }

    public int IntProperty { get; init; }

    public int[]? IntArrayProperty { get; init; }

    public string? NullProperty { get; init; }

    public string? UnsetProperty { get; init; }
}

public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    static virtual string StringProperty => "Default";

    static virtual int IntProperty { get; }

    static virtual int[] IntArrayProperty { get; } = [];

    static virtual string? NullProperty { get; }

    static virtual string? UnsetProperty { get; }
}

public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotSupportedException();
}

[TestTransportSignal(StringProperty = "Test", IntProperty = 1, IntArrayProperty = [1, 2, 3], NullProperty = null)]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}
