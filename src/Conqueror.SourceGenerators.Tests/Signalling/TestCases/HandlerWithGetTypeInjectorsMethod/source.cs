using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.HandlerWithGetTypeInjectorsMethod;

[Signal]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();

    static IEnumerable<ISignalHandlerTypesInjector> ISignalHandler.GetTypeInjectors() => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}

public partial class TestSignalHandler : ISignalHandler;
