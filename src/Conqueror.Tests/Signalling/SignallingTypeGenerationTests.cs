// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;

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
                              .WithPublisher(b => b.UseInProcessWithSequentialBroadcastingStrategy())
                              .Handle(new(10));
    }

    [Signal]
    public sealed partial record TestSignal(int Payload);

    // generated
    public sealed partial record TestSignal : ISignal<TestSignal>
    {
        public static SignalTypes<TestSignal, IHandler> T => SignalTypes<TestSignal, IHandler>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestSignal? ISignal<TestSignal>.EmptyInstance => null;

        public static IDefaultSignalTypesInjector DefaultTypeInjector
            => DefaultSignalTypesInjector<TestSignal, IHandler, IHandler.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<ISignalTypesInjector> ISignal<TestSignal>.TypeInjectors
            => ISignalTypesInjector.GetTypeInjectorsForSignalType<TestSignal>();

        public interface IHandler : IGeneratedSignalHandler<TestSignal, IHandler>
        {
            Task Handle(TestSignal signal, CancellationToken cancellationToken = default);

            static Task IGeneratedSignalHandler<TestSignal, IHandler>.Invoke(IHandler handler, TestSignal signal, CancellationToken cancellationToken)
                => handler.Handle(signal, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedSignalHandlerAdapter<TestSignal, IHandler, Adapter>, IHandler;
        }
    }

    [Signal]
    public sealed partial record TestSignal2(int Payload);

    [Signal]
    public sealed partial record GenericTestSignal<TPayload>(TPayload Payload);

    // generated
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "testing")]
    public sealed partial record GenericTestSignal<TPayload> : ISignal<GenericTestSignal<TPayload>>
    {
        public static SignalTypes<GenericTestSignal<TPayload>, IHandler> T => SignalTypes<GenericTestSignal<TPayload>, IHandler>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static GenericTestSignal<TPayload>? ISignal<GenericTestSignal<TPayload>>.EmptyInstance => null;

        public static IDefaultSignalTypesInjector DefaultTypeInjector
            => DefaultSignalTypesInjector<GenericTestSignal<TPayload>, IHandler, IHandler.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<ISignalTypesInjector> ISignal<GenericTestSignal<TPayload>>.TypeInjectors
            => ISignalTypesInjector.GetTypeInjectorsForSignalType<GenericTestSignal<TPayload>>();

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : IGeneratedSignalHandler<GenericTestSignal<TPayload>, IHandler>
        {
            Task Handle(GenericTestSignal<TPayload> signal, CancellationToken cancellationToken = default);

            static Task IGeneratedSignalHandler<GenericTestSignal<TPayload>, IHandler>.Invoke(IHandler handler, GenericTestSignal<TPayload> signal, CancellationToken cancellationToken)
                => handler.Handle(signal, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedSignalHandlerAdapter<GenericTestSignal<TPayload>, IHandler, Adapter>, IHandler;
        }
    }

    private sealed class TestSignalHandler : TestSignal.IHandler,
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

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => pipeline.UseTest().UseTest();

        public static void ConfigureInProcessReceiver<T>(IInProcessSignalReceiver<T> receiver)
            where T : class, ISignal<T>
        {
            // nothing to do
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
