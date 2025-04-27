using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.GenericWithHierarchy;

[Signal]
public abstract partial record TestSignal<TFirst, TSecond>;

[Signal]
public partial record TestSignal : TestSignal<string, int>;

public partial class TestSignalHandler<TFirst, TSecond> : TestSignal<TFirst, TSecond>.IHandler
{
    public Task Handle(TestSignal<TFirst, TSecond> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestSignalHandler2 : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal<TFirst, TSecond>
{
    public partial interface IHandler;
}

public partial record TestSignal
{
    public new partial interface IHandler;
}
