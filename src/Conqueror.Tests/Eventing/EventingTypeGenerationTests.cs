// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;

#pragma warning disable SA1302
#pragma warning disable CA1715

namespace Conqueror.Tests.Eventing;

public sealed partial class EventingTypeGenerationTests
{
    [Test]
    public async Task GivenEventNotificationTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services.AddConqueror()
                               .AddEventNotificationHandler<TestEventNotificationHandler>()
                               .BuildServiceProvider();

        var notificationPublishers = provider.GetRequiredService<IEventNotificationPublishers>();

        await notificationPublishers.For(TestEventNotification.T)
                                    .WithPipeline(p => p.UseTest().UseTest())
                                    .WithPublisher(b => b.UseInProcessWithSequentialBroadcastingStrategy())
                                    .Handle(new(10));
    }

    [EventNotification]
    public sealed partial record TestEventNotification(int Payload);

    // generated
    public sealed partial record TestEventNotification : IEventNotification<TestEventNotification>
    {
        public static EventNotificationTypes<TestEventNotification, IHandler> T => EventNotificationTypes<TestEventNotification, IHandler>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestEventNotification? IEventNotification<TestEventNotification>.EmptyInstance => null;

        public static IDefaultEventNotificationTypesInjector DefaultTypeInjector
            => DefaultEventNotificationTypesInjector<TestEventNotification, IHandler, IHandler.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<IEventNotificationTypesInjector> IEventNotification<TestEventNotification>.TypeInjectors
            => IEventNotificationTypesInjector.GetTypeInjectorsForEventNotificationType<TestEventNotification>();

        public interface IHandler : IGeneratedEventNotificationHandler<TestEventNotification, IHandler>
        {
            Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default);

            static Task IGeneratedEventNotificationHandler<TestEventNotification, IHandler>.Invoke(IHandler handler, TestEventNotification notification, CancellationToken cancellationToken)
                => handler.Handle(notification, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedEventNotificationHandlerAdapter<TestEventNotification, IHandler, Adapter>, IHandler;
        }
    }

    [EventNotification]
    public sealed partial record TestEventNotification2(int Payload);

    [EventNotification]
    public sealed partial record GenericTestEventNotification<TPayload>(TPayload Payload);

    // generated
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "testing")]
    public sealed partial record GenericTestEventNotification<TPayload> : IEventNotification<GenericTestEventNotification<TPayload>>
    {
        public static EventNotificationTypes<GenericTestEventNotification<TPayload>, IHandler> T => EventNotificationTypes<GenericTestEventNotification<TPayload>, IHandler>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static GenericTestEventNotification<TPayload>? IEventNotification<GenericTestEventNotification<TPayload>>.EmptyInstance => null;

        public static IDefaultEventNotificationTypesInjector DefaultTypeInjector
            => DefaultEventNotificationTypesInjector<GenericTestEventNotification<TPayload>, IHandler, IHandler.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<IEventNotificationTypesInjector> IEventNotification<GenericTestEventNotification<TPayload>>.TypeInjectors
            => IEventNotificationTypesInjector.GetTypeInjectorsForEventNotificationType<GenericTestEventNotification<TPayload>>();

        [SuppressMessage("ReSharper", "PartialTypeWithSinglePart", Justification = "emulating generator output")]
        public partial interface IHandler : IGeneratedEventNotificationHandler<GenericTestEventNotification<TPayload>, IHandler>
        {
            Task Handle(GenericTestEventNotification<TPayload> notification, CancellationToken cancellationToken = default);

            static Task IGeneratedEventNotificationHandler<GenericTestEventNotification<TPayload>, IHandler>.Invoke(IHandler handler, GenericTestEventNotification<TPayload> notification, CancellationToken cancellationToken)
                => handler.Handle(notification, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedEventNotificationHandlerAdapter<GenericTestEventNotification<TPayload>, IHandler, Adapter>, IHandler;
        }
    }

    private sealed class TestEventNotificationHandler : TestEventNotification.IHandler,
                                                        TestEventNotification2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public async Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => pipeline.UseTest().UseTest();

        public static void ConfigureInProcessReceiver<T>(IInProcessEventNotificationReceiver<T> receiver)
            where T : class, IEventNotification<T>
        {
            // nothing to do
        }
    }
}

public static class EventNotificationTypeGenerationTestsPipelineExtensions
{
    public static IEventNotificationPipeline<TEventNotification> UseTest<TEventNotification>(this IEventNotificationPipeline<TEventNotification> pipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return pipeline.Use(ctx => ctx.Next(ctx.EventNotification, ctx.CancellationToken));
    }
}
