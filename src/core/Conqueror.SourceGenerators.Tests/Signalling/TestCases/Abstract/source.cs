namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.Abstract;

using System;
using System.Threading;
using System.Threading.Tasks;

[Signal]
public abstract partial record TestSignal;

public abstract partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}
