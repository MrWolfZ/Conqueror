// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

// we simulate the generator output here
#pragma warning disable SA1201, SA1302, CA1715, S2333, RCS1018

namespace Conqueror.Tests.Signalling;

public sealed partial class SignalTypeGenerationTests
{
    [Test]
    public async Task GivenSignalTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services.AddSignalHandler<TestSignalHandler>().BuildServiceProvider();

        var signalPublishers = provider.GetRequiredService<ISignalPublishers>();

        await Assert.ThatAsync(
            async () =>
                await signalPublishers
                    .For(TestSignal.T)
                    .WithPipeline(p => p.UseTest().UseTest())
                    .WithTransport(b => b.UseInProcess())
                    .Handle(new(Payload: 10), CancellationToken.None),
            Throws.Nothing
        );
    }

    [Signal]
    public sealed partial record TestSignal(int Payload);

    // generated
    public sealed partial record TestSignal : ISignal<TestSignal>
    {
        public static SignalTypes<TestSignal, IHandler> T => new();

        static ISignalHandlerTypesInjector ISignal<TestSignal>.CoreTypesInjector { get; } =
            IHandler.CreateCoreTypesInjector();

        static TestSignal? ISignal<TestSignal>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> ISignal<TestSignal>.PublicConstructors =>
            typeof(TestSignal).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> ISignal<TestSignal>.PublicProperties =>
            typeof(TestSignal).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static Task ISignal<TestSignal>.InvokeHandler<TIHandler>(
            TIHandler handler,
            TestSignal signal,
            CancellationToken cancellationToken
        ) => ((IHandler)handler).Handle(signal, cancellationToken);

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : ISignalHandler<TestSignal, IHandler, IHandler.Proxy>
        {
            Task Handle(TestSignal signal, CancellationToken cancellationToken = default);

            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : SignalHandlerProxy<TestSignal, IHandler, Proxy>, IHandler;
        }
    }

    [Signal]
    public sealed partial record TestSignal2(int Payload);

    // generated
    public sealed partial record TestSignal2 : ISignal<TestSignal2>
    {
        public static SignalTypes<TestSignal2, IHandler> T => new();

        static ISignalHandlerTypesInjector ISignal<TestSignal2>.CoreTypesInjector { get; } =
            IHandler.CreateCoreTypesInjector();

        static TestSignal2? ISignal<TestSignal2>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> ISignal<TestSignal2>.PublicConstructors =>
            typeof(TestSignal2).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> ISignal<TestSignal2>.PublicProperties =>
            typeof(TestSignal2).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static Task ISignal<TestSignal2>.InvokeHandler<TIHandler>(
            TIHandler handler,
            TestSignal2 signal,
            CancellationToken cancellationToken
        ) => ((IHandler)handler).Handle(signal, cancellationToken);

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : ISignalHandler<TestSignal2, IHandler, IHandler.Proxy>
        {
            Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default);

            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : SignalHandlerProxy<TestSignal2, IHandler, Proxy>, IHandler;
        }
    }

    [Signal]
    public sealed partial record GenericTestSignal<TPayload>(TPayload Payload);

    // generated
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "testing")]
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "testing")]
    public sealed partial record GenericTestSignal<TPayload> : ISignal<GenericTestSignal<TPayload>>
    {
        public static SignalTypes<GenericTestSignal<TPayload>, IHandler> T => new();

        static ISignalHandlerTypesInjector ISignal<GenericTestSignal<TPayload>>.CoreTypesInjector { get; } =
            IHandler.CreateCoreTypesInjector();

        static GenericTestSignal<TPayload>? ISignal<GenericTestSignal<TPayload>>.EmptyInstance => null;

        static IEnumerable<PropertyInfo> ISignal<GenericTestSignal<TPayload>>.PublicProperties =>
            typeof(GenericTestSignal<TPayload>).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static IEnumerable<ConstructorInfo> ISignal<GenericTestSignal<TPayload>>.PublicConstructors =>
            typeof(TestSignal).GetConstructors(BindingFlags.Public);

        static Task ISignal<GenericTestSignal<TPayload>>.InvokeHandler<TIHandler>(
            TIHandler handler,
            GenericTestSignal<TPayload> signal,
            CancellationToken cancellationToken
        ) => ((IHandler)handler).Handle(signal, cancellationToken);

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : ISignalHandler<GenericTestSignal<TPayload>, IHandler, IHandler.Proxy>
        {
            Task Handle(GenericTestSignal<TPayload> signal, CancellationToken cancellationToken = default);

            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : SignalHandlerProxy<GenericTestSignal<TPayload>, IHandler, Proxy>, IHandler;
        }
    }

    private sealed partial class TestSignalHandler : TestSignal.IHandler, TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default) =>
            await Task.CompletedTask;

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default) =>
            await Task.CompletedTask;

        static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) => pipeline.UseTest().UseTest();

        static void ISignalHandler.ConfigureInProcessReceiver(IInProcessSignalReceiver receiver)
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
        where TSignal : class, ISignal<TSignal> => pipeline.Use(ctx => ctx.Next(ctx.Signal, ctx.CancellationToken));
}
