#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Signalling;
using Signalling.WithCustomTransportWithHierarchy;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithHierarchy
{
    [TestTransportSignal]
    public abstract partial record TestSignal(int Payload);

    [TestTransportSignal]
    public sealed partial record TestSignalSub(int Payload) : TestSignal(Payload);

    public partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public partial class TestSignalSubHandler : TestSignalSub.IHandler
    {
        public Task Handle(TestSignalSub message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace Signalling.WithCustomTransportWithHierarchy
{
    [SignalTransport(Prefix = "TestTransport", Namespace = "Signalling.WithCustomTransportWithHierarchy")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportSignalAttribute : Attribute;

    public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
        where TSignal : class, ITestTransportSignal<TSignal>;

    public interface ITestTransportSignalHandler<TSignal, TIHandler>
        where TSignal : class, ITestTransportSignal<TSignal>
        where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
    {
        static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler
            => throw new NotSupportedException();
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
