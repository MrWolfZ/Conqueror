#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Signalling;

namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithSignalTypeOverride
{
    using WithCustomTransportWithSignalTypeOverrideCustomTransport;

    [CustomTestTransportSignal(ExtraProperty = "Test")]
    public partial record TestSignal;

    public record TestSignalResponse;

    public partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

namespace WithCustomTransportWithSignalTypeOverrideOriginalTransport
{
    [SignalTransport(Prefix = "TestTransport", Namespace = "WithCustomTransportWithSignalTypeOverrideOriginalTransport")]
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

namespace WithCustomTransportWithSignalTypeOverrideCustomTransport
{
    [SignalTransport(Prefix = "TestTransport", Namespace = "WithCustomTransportWithSignalTypeOverrideOriginalTransport",
    FullyQualifiedSignalTypeName = "WithCustomTransportWithSignalTypeOverrideCustomTransport.ICustomTestTransportSignal")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomTestTransportSignalAttribute : Attribute
    {
        public string? ExtraProperty { get; init; }
    }

    public interface ICustomTestTransportSignal<out TSignal> : WithCustomTransportWithSignalTypeOverrideOriginalTransport.ITestTransportSignal<TSignal>
        where TSignal : class, ICustomTestTransportSignal<TSignal>
    {
        static string WithCustomTransportWithSignalTypeOverrideOriginalTransport.ITestTransportSignal<TSignal>.StringProperty { get; } = TSignal.ExtraProperty ?? "Default";

        static virtual string? ExtraProperty { get; }
    }
}

// make the compiler happy during design time
namespace Conqueror.SourceGenerators.Tests.Signalling.TestCases.WithCustomTransportWithSignalTypeOverride
{
    public partial record TestSignal
    {
        public partial interface IHandler;
    }
}
