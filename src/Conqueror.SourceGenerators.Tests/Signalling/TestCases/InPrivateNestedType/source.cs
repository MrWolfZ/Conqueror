using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.InPrivateNestedType;

public partial class Container
{
    public void Method()
    {
        // nothing to do
    }

    private sealed partial class PrivateClass
    {
        [Signal]
        public partial record TestSignal;

        public record TestSignalResponse;

        public partial class TestSignalHandler : TestSignal.IHandler
        {
            public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}

// make the compiler happy during design time
public partial class Container
{
    private sealed partial class PrivateClass
    {
        public partial record TestSignal
        {
            public partial interface IHandler;
        }
    }
}
