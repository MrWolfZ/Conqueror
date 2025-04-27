using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.InSameNestedTypeProtected;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    [Signal]
    protected partial record TestSignal;

    protected record TestSignalResponse;

    protected partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
public partial class Container
{
    protected partial record TestSignal
    {
        public partial interface IHandler;
    }
}
