// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;
using System.Reflection;

#pragma warning disable SA1302
#pragma warning disable CA1715

namespace Conqueror.Tests.Signalling;

public sealed partial class SignallingTypeGenerationTests
{
    [Test]
    public async Task GivenSignalTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services.AddConqueror()
                               .AddSignalHandler<TestSignalHandler>()
                               .BuildServiceProvider();

        var signalPublishers = provider.GetRequiredService<ISignalPublishers>();

        await signalPublishers.For(TestSignal.T)
                              .WithPipeline(p => p.UseTest().UseTest())
                              .WithTransport(b => b.UseInProcessWithSequentialBroadcastingStrategy())
                              .Handle(new(10));
    }

    [Signal]
    public sealed partial record TestSignal(int Payload);

    // generated
    public sealed partial record TestSignal : ISignal<TestSignal>
    {
        public static SignalTypes<TestSignal, IHandler> T => new();

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : ISignalHandler<TestSignal, IHandler, IHandler.Proxy>
        {
            Task Handle(TestSignal signal, CancellationToken cancellationToken = default);

            static Task ISignalHandler<TestSignal, IHandler>.Invoke(IHandler handler, TestSignal signal, CancellationToken cancellationToken)
                => handler.Handle(signal, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Proxy : SignalHandlerProxy<TestSignal, IHandler, Proxy>, IHandler;
        }

        static TestSignal? ISignal<TestSignal>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> ISignal<TestSignal>.PublicConstructors
            => typeof(TestSignal).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> ISignal<TestSignal>.PublicProperties
            => typeof(TestSignal).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    [Signal]
    public sealed partial record TestSignal2(int Payload);

    [Signal]
    public sealed partial record GenericTestSignal<TPayload>(TPayload Payload);

    // generated
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "testing")]
    public sealed partial record GenericTestSignal<TPayload> : ISignal<GenericTestSignal<TPayload>>
    {
        public static SignalTypes<GenericTestSignal<TPayload>, IHandler> T => new();

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : ISignalHandler<GenericTestSignal<TPayload>, IHandler, IHandler.Proxy>
        {
            Task Handle(GenericTestSignal<TPayload> signal, CancellationToken cancellationToken = default);

            static Task ISignalHandler<GenericTestSignal<TPayload>, IHandler>.Invoke(IHandler handler, GenericTestSignal<TPayload> signal, CancellationToken cancellationToken)
                => handler.Handle(signal, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Proxy : SignalHandlerProxy<GenericTestSignal<TPayload>, IHandler, Proxy>, IHandler;
        }

        static GenericTestSignal<TPayload>? ISignal<GenericTestSignal<TPayload>>.EmptyInstance => null;

        static IEnumerable<PropertyInfo> ISignal<GenericTestSignal<TPayload>>.PublicProperties
            => typeof(GenericTestSignal<TPayload>).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static IEnumerable<ConstructorInfo> ISignal<GenericTestSignal<TPayload>>.PublicConstructors
            => typeof(TestSignal).GetConstructors(BindingFlags.Public);
    }

    private sealed partial class TestSignalHandler : TestSignal.IHandler,
                                                     TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            => pipeline.UseTest().UseTest();

        static void ISignalHandler.ConfigureInProcessReceiver<T>(IInProcessSignalReceiver<T> receiver)
        {
            // nothing to do
        }
    }

    // generated
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "testing")]
    private sealed partial class TestSignalHandler
    {
        static IEnumerable<ISignalHandlerTypesInjector> ISignalHandler.GetTypeInjectors()
        {
            yield return TestSignal.IHandler.CreateCoreTypesInjector<TestSignalHandler>();
            yield return TestSignal2.IHandler.CreateCoreTypesInjector<TestSignalHandler>();
        }
    }
}

public static class SignalTypeGenerationTestsPipelineExtensions
{
    public static ISignalPipeline<TSignal> UseTest<TSignal>(this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.Use(ctx => ctx.Next(ctx.Signal, ctx.CancellationToken));
    }
}
