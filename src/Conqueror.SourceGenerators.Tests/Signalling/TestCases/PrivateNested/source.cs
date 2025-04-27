using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.PrivateNested;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Signal]
    private partial record TestSignal;

    private record TestSignalResponse;

    private partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    private partial record TestSignal
    {
        public partial interface IHandler;
    }
}
