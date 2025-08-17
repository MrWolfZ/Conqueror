#nullable enable

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithHierarchy
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Signalling.WithCustomTransportWithHierarchy;

    [TestTransportSignal]
    public abstract partial record TestSignal(int Payload);

    [TestTransportSignal]
    public sealed partial record TestSignalSub(int Payload) : TestSignal(Payload);

    public partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    public partial class TestSignalSubHandler : TestSignalSub.IHandler
    {
        public Task Handle(TestSignalSub message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

namespace Signalling.WithCustomTransportWithHierarchy
{
    using System;
    using Conqueror;
    using Conqueror.Signalling;

    [SignalTransport(Prefix = "TestTransport", Namespace = "Signalling.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportSignalAttribute : Attribute;

    public interface ITestTransportSignal<TSignal> : ISignal<TSignal>
        where TSignal : class, ITestTransportSignal<TSignal>;

    public interface ITestTransportSignalHandler;

    public interface ITestTransportSignalHandler<TSignal, TIHandler>
        where TSignal : class, ITestTransportSignal<TSignal>
        where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
    {
        static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithHierarchy
{
    public partial record TestSignal
    {
        public partial interface IHandler;
    }

    public partial record TestSignalSub
    {
        public new partial interface IHandler;
    }
}
