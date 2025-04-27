using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.HandlerBaseClassForMultipleSignalTypes;

[Signal]
public partial record TestSignal;

[Signal]
public partial record TestSignal2;

public partial record FakeSignal
{
    public interface IHandler;
}

public abstract class BaseHandler;

public partial class TestSignalHandler : BaseHandler,
                                         TestSignal.IHandler,
                                         TestSignal2.IHandler,
                                         FakeSignal.IHandler
{
    public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task Handle(TestSignal2 signal, CancellationToken cancellationToken) => throw new NotSupportedException();
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

public partial class TestSignalHandler : ISignalHandler;
