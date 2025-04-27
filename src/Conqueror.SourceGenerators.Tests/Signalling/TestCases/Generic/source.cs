using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.Generic;

[Signal]
public partial record TestSignal<TFirst, TSecond>;

public partial class TestSignalHandler<TFirst, TSecond> : TestSignal<TFirst, TSecond>.IHandler
{
    public Task Handle(TestSignal<TFirst, TSecond> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestSignalHandler2 : TestSignal<string, int>.IHandler
{
    public Task Handle(TestSignal<string, int> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal<TFirst, TSecond>
{
    public partial interface IHandler;
}
