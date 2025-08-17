namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.HandlerWithModuleInitializerMethod;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[Signal]
public partial record TestSignal;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();

    [ModuleInitializer]
    public static void ModuleInitializer()
    {
        // Method intentionally left empty.
    }
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;
}

public partial class TestSignalHandler : ISignalHandler;
