#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Signalling;
using Signalling.WithCustomTransports.Transport1;
using Signalling.WithCustomTransports.Transport2;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransports
{
    [Signal]
    [TestTransportSignal(StringProperty = "Test")]
    [TestTransport2Signal(StringProperty = "Test2")]
    public partial record TestSignal;

    public partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace Signalling.WithCustomTransports.Transport1
{
    [SignalTransport(Prefix = "TestTransport", Namespace = "Signalling.WithCustomTransports.Transport1")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransportSignalAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
        where TSignal : class, ITestTransportSignal<TSignal>
    {
        static virtual string StringProperty => "Default";
    }

    public interface ITestTransportSignalHandler<TSignal, TIHandler>
        where TSignal : class, ITestTransportSignal<TSignal>
        where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
    {
        static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
            where THandler : class, TIHandler
            => throw new NotSupportedException();
    }
}

namespace Signalling.WithCustomTransports.Transport2
{
    [SignalTransport(Prefix = "TestTransport2", Namespace = "Signalling.WithCustomTransports.Transport2")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransport2SignalAttribute : Attribute
    {
        public string? StringProperty { get; init; }
    }

    public interface ITestTransport2Signal<out TSignal> : ISignal<TSignal>
        where TSignal : class, ITestTransport2Signal<TSignal>
    {
        static virtual string? StringProperty { get; }
    }

    public interface ITestTransport2SignalHandler<TSignal, TIHandler>
        where TSignal : class, ITestTransport2Signal<TSignal>
        where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
    {
        static ISignalHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
            where THandler : class, TIHandler
            => throw new NotSupportedException();
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransports
{
    public partial record TestSignal
    {
        public partial interface IHandler;
    }
}
