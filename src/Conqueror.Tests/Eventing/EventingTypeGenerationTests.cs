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
                                    .AsIHandler()
                                    .Handle(new(10));
    }

    public sealed partial record TestEventNotification(int Payload);

    // generated
    public sealed partial record TestEventNotification : IEventNotification<TestEventNotification>
    {
        public static EventNotificationTypes<TestEventNotification> T => EventNotificationTypes<TestEventNotification>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestEventNotification? IEventNotification<TestEventNotification>.EmptyInstance => null;

        public static IDefaultEventNotificationTypesInjector DefaultTypeInjector
            => DefaultEventNotificationTypesInjector<TestEventNotification, IHandler, IHandler.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<IEventNotificationTypesInjector> IEventNotification<TestEventNotification>.TypeInjectors
            => IEventNotificationTypesInjector.GetTypeInjectorsForEventNotificationType<TestEventNotification>();

        public interface IHandler : IGeneratedEventNotificationHandler<TestEventNotification>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedEventNotificationHandlerAdapter<TestEventNotification>, IHandler;
        }
    }

    [EventNotification]
    public sealed partial record TestEventNotification2(int Payload);

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

        public static void ConfigureReceiver(IEventNotificationReceiver receiver)
        {
            // nothing to do
        }

        public static Task ConfigureReceiverAsync(IEventNotificationReceiver receiver)
        {
            return Task.CompletedTask;
        }
    }
}

public static class TestEventNotificationHandlerExtensions_2440209043122230735
{
    public static EventingTypeGenerationTests.TestEventNotification.IHandler AsIHandler(this IEventNotificationHandler<EventingTypeGenerationTests.TestEventNotification> handler)
        => handler.AsIHandler<EventingTypeGenerationTests.TestEventNotification, EventingTypeGenerationTests.TestEventNotification.IHandler>();
}

public static class EventNotificationTypeGenerationTestsPipelineExtensions
{
    public static IEventNotificationPipeline<TEventNotification> UseTest<TEventNotification>(this IEventNotificationPipeline<TEventNotification> pipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return pipeline.Use(ctx => ctx.Next(ctx.EventNotification, ctx.CancellationToken));
    }
}
