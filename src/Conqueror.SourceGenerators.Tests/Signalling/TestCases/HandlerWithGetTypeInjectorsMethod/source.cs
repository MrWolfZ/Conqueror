namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.HandlerWithGetTypeInjectorsMethod;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[Signal]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    static IEnumerable<ISignalHandlerTypesInjector> ISignalHandler.GetTypeInjectors() =>
        throw new NotSupportedException();

    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}

public partial class TestSignalHandler : ISignalHandler;
