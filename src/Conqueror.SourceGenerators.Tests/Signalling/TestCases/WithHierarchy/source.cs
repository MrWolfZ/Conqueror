using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithHierarchy;

[Signal]
public partial record TestSignal(int Payload);

[Signal]
public partial record TestSignalSub(int Payload) : TestSignal(Payload);

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
