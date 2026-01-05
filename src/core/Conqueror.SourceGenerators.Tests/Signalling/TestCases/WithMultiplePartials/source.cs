namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithMultiplePartials;

using System;
using System.Threading;
using System.Threading.Tasks;

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
    public static ISignalHandlerTypesInjector CoreTypesInjector => null!;

    public static TestSignal? EmptyInstance => null;

    public static System.Collections.Generic.IEnumerable<System.Reflection.ConstructorInfo> PublicConstructors => null!;

    public static System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> PublicProperties => null!;

    static Task ISignal<TestSignal>.InvokeHandler<TIHandler>(
        TIHandler handler,
        TestSignal signal,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public partial interface IHandler;
}
