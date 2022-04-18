using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Eventing.Tests
{
    public sealed class EventObserverPolymorphismTests
    {
        [Test]
        public async Task GivenEventObserver_ObserverIsCalledForSubTypesOfEventTypeWhenResolvedDirectly()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<PolymorphicTestEventBaseObserver>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();

            var evt = new PolymorphicTestEvent();

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));
        }

        [Test]
        public async Task GivenEventObserver_ObserverIsCalledForSubTypesOfEventTypeViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<PolymorphicTestEventBaseObserver>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new PolymorphicTestEvent();

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));
        }

        private sealed record PolymorphicTestEvent : PolymorphicTestEventBase;

        private abstract record PolymorphicTestEventBase;

        private sealed class PolymorphicTestEventBaseObserver : IEventObserver<PolymorphicTestEventBase>
        {
            private readonly TestObservations observations;

            public PolymorphicTestEventBaseObserver(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task HandleEvent(PolymorphicTestEventBase evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Events.Add(evt);
            }
        }

        private sealed class TestObservations
        {
            public List<PolymorphicTestEventBase> Events { get; } = new();
        }
    }
}
