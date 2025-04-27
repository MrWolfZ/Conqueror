using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithMultiplePartials;

[Signal]
public partial record TestSignal;

public partial record TestSignal : ISignal<TestSignal>;

public partial class TestSignalHandler : TestSignal.IHandler
{
    public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestSignal
{
    public partial interface IHandler;

    public static TestSignal? EmptyInstance => null;

    public static System.Collections.Generic.IEnumerable<System.Reflection.ConstructorInfo> PublicConstructors => null!;

    public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> PublicProperties => null!;
}
