using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithUnrelatedAttributeWithSignalSuffix;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MySignalAttribute : Attribute;

[MySignal]
public partial record TestSignal
{
    public partial interface IHandler;
}

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}
