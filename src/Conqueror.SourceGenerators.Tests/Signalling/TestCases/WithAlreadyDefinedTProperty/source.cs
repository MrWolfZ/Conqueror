using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithAlreadyDefinedTProperty;

[Signal]
public partial record TestSignal
{
    public string T { get; init; } = "test";

    public partial interface IHandler;
}

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}
