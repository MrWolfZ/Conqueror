using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.MultipleSignalTypes;

[Signal]
public partial record TestSignal;

[Signal]
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
