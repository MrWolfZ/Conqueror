﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Eventing.Tests
{
    public sealed class EventMiddlewareFunctionalityTests
    {
        [Test]
        public async Task GivenObserverWithNoObserverMiddleware_MiddlewareIsNotCalledViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.Empty);
            Assert.That(observations.MiddlewareTypes, Is.Empty);
        }

        [Test]
        public async Task GivenObserverWithNoObserverMiddleware_MiddlewareIsNotCalledViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.Empty);
            Assert.That(observations.MiddlewareTypes, Is.Empty);
        }

        [Test]
        public async Task GivenObserverWithSingleAppliedObserverMiddleware_MiddlewareIsCalledWithEventViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventObserverMiddleware) }));
        }

        [Test]
        public async Task GivenObserverWithSingleAppliedObserverMiddlewareWithParameter_MiddlewareIsCalledWithParameterViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddlewareWithParameter>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new() { Payload = 10 }, CancellationToken.None);

            Assert.That(observations.AttributesFromMiddlewares, Is.EquivalentTo(new[] { new TestEventObserverMiddlewareAttribute { Parameter = 10 } }));
        }

        [Test]
        public async Task GivenObserverWithSingleAppliedObserverMiddleware_MiddlewareIsCalledWithEventViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventObserverMiddleware) }));
        }

        [Test]
        public async Task GivenObserverWithSingleAppliedObserverMiddlewareWithParameter_MiddlewareIsCalledWithParameterViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddlewareWithParameter>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            await publisher.PublishEvent(new TestEvent { Payload = 10 }, CancellationToken.None);

            Assert.That(observations.AttributesFromMiddlewares, Is.EquivalentTo(new[] { new TestEventObserverMiddlewareAttribute { Parameter = 10 } }));
        }

        [Test]
        public async Task GivenObserverWithMultipleAppliedObserverMiddlewares_MiddlewaresAreCalledWithEventViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2) }));
        }

        [Test]
        public async Task GivenObserverWithMultipleAppliedObserverMiddlewares_MiddlewaresAreCalledWithEventViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2) }));
        }

        [Test]
        public async Task GivenObserverWithSinglePublisherMiddleware_MiddlewareIsCalledWithEventViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventPublisherMiddleware) }));
        }

        [Test]
        public async Task GivenObserverWithSinglePublisherMiddleware_MiddlewareIsCalledWithEventViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventPublisherMiddleware) }));
        }

        [Test]
        public async Task GivenObserverWithMultiplePublisherMiddlewares_MiddlewaresAreCalledWithEventViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventPublisherMiddleware2) }));
        }

        [Test]
        public async Task GivenObserverWithMultiplePublisherMiddlewares_MiddlewaresAreCalledWithEventViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventPublisherMiddleware2) }));
        }

        [Test]
        public async Task GivenMultipleObserversWithDifferentAppliedObserverMiddlewares_MiddlewaresAreCalledForEachObserverRespectivelyViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2) }));
        }

        [Test]
        public async Task GivenMultipleObserversWithDifferentAppliedObserverMiddlewares_MiddlewaresAreCalledForEachObserverRespectivelyViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2) }));
        }

        [Test]
        public async Task GivenObserverWithSingleAppliedObserverMiddlewareAndSinglePublisherMiddleware_MiddlewaresAreCalledWithEventViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventObserverMiddleware) }));
        }

        [Test]
        public async Task GivenObserverWithSingleAppliedObserverMiddlewareAndSinglePublisherMiddleware_MiddlewaresAreCalledWithEventViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventObserverMiddleware) }));
        }

        [Test]
        public async Task GivenMultipleObserversWithDifferentAppliedObserverMiddlewaresAndSinglePublisherMiddleware_MiddlewaresAreCalledViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventPublisherMiddleware), typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenMultipleObserversWithDifferentAppliedObserverMiddlewaresAndSinglePublisherMiddleware_MiddlewaresAreCalledViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventPublisherMiddleware), typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenObserverWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithRetryMiddleware>()
                        .AddTransient<TestEventObserverRetryMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventObserverRetryMiddleware),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenObserverWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithRetryMiddleware>()
                        .AddTransient<TestEventObserverRetryMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventObserverRetryMiddleware),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenPublisherRetryMiddleware_MiddlewaresAreCalledMultipleTimesViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventPublisherRetryMiddleware),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventPublisherMiddleware2),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventPublisherMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenPublisherRetryMiddleware_MiddlewaresAreCalledMultipleTimesViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithoutMiddlewares>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt, evt, evt, evt, evt }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventPublisherRetryMiddleware),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventPublisherMiddleware2),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventPublisherMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenObserverWithRetryMiddlewareAndPublisherRetryMiddleware_MiddlewaresAreCalledMultipleTimesViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithRetryMiddleware>()
                        .AddTransient<TestEventObserverRetryMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            var evt = new TestEvent { Payload = 10 };

            await observer.HandleEvent(evt, CancellationToken.None);

            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventPublisherRetryMiddleware),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventObserverRetryMiddleware),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventObserverRetryMiddleware),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenObserverWithRetryMiddlewareAndPublisherRetryMiddleware_MiddlewaresAreCalledMultipleTimesViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithRetryMiddleware>()
                        .AddTransient<TestEventObserverRetryMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventObserverMiddleware2>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            var evt = new TestEvent { Payload = 10 };

            await publisher.PublishEvent(evt, CancellationToken.None);

            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestEventPublisherRetryMiddleware),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventObserverRetryMiddleware),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventPublisherMiddleware),
                            typeof(TestEventObserverRetryMiddleware),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                            typeof(TestEventObserverMiddleware),
                            typeof(TestEventObserverMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenObserverAndPublisherMiddlewares_MiddlewaresCanChangeTheEvent()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestEventObserverMiddleware>()
                        .AddTransient<MutatingTestEventObserverMiddleware2>()
                        .AddTransient<MutatingTestEventPublisherMiddleware>()
                        .AddTransient<MutatingTestEventPublisherMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            await publisher.PublishEvent(new TestEvent { Payload = 0 }, CancellationToken.None);

            var evt1 = new TestEvent { Payload = 0 };
            var evt2 = new TestEvent { Payload = 1 };
            var evt3 = new TestEvent { Payload = 3 };
            var evt4 = new TestEvent { Payload = 7 };
            var evt5 = new TestEvent { Payload = 15 };

            Assert.That(observations.EventsFromMiddlewares, Is.EquivalentTo(new[] { evt1, evt2, evt3, evt4 }));
            Assert.That(observations.EventsFromObservers, Is.EquivalentTo(new[] { evt5 }));
        }

        [Test]
        public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalledViaDirectObserver()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
            using var tokenSource = new CancellationTokenSource();

            await observer.HandleEvent(new() { Payload = 10 }, tokenSource.Token);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
        }

        [Test]
        public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalledViaPublisher()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithSingleMiddleware>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();
            using var tokenSource = new CancellationTokenSource();

            await publisher.PublishEvent(new TestEvent { Payload = 10 }, tokenSource.Token);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
        }

        [Test]
        public async Task GivenObserverAndPublisherMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestEventObserverMiddleware>()
                        .AddTransient<MutatingTestEventObserverMiddleware2>()
                        .AddTransient<MutatingTestEventPublisherMiddleware>()
                        .AddTransient<MutatingTestEventPublisherMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var publisher = provider.GetRequiredService<IEventPublisher>();

            await publisher.PublishEvent(new TestEvent { Payload = 0 }, tokens.CancellationTokens[0]);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(4)));
            Assert.That(observations.CancellationTokensFromObservers, Is.EquivalentTo(new[] { tokens.CancellationTokens[4] }));
        }

        [Test]
        public void InvalidMiddlewares()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddTransient<TestEventObserverMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddScoped<TestEventObserverMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddSingleton<TestEventObserverMiddlewareWithMultipleInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddTransient<TestEventObserverMiddlewareWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddScoped<TestEventObserverMiddlewareWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddSingleton<TestEventObserverMiddlewareWithoutInterfaces>().ConfigureConqueror());
        }

        private sealed record TestEvent
        {
            public int Payload { get; init; }
        }

        private sealed class TestEventObserverWithSingleMiddleware : IEventObserver<TestEvent>
        {
            private readonly TestObservations observations;

            public TestEventObserverWithSingleMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestEventObserverMiddleware]
            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.EventsFromObservers.Add(evt);
                observations.CancellationTokensFromObservers.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverWithSingleMiddlewareWithParameter : IEventObserver<TestEvent>
        {
            private readonly TestObservations observations;

            public TestEventObserverWithSingleMiddlewareWithParameter(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestEventObserverMiddleware(Parameter = 10)]
            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.EventsFromObservers.Add(evt);
                observations.CancellationTokensFromObservers.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverWithMultipleMiddlewares : IEventObserver<TestEvent>
        {
            private readonly TestObservations observations;

            public TestEventObserverWithMultipleMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestEventObserverMiddleware]
            [TestEventObserverMiddleware2]
            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.EventsFromObservers.Add(evt);
                observations.CancellationTokensFromObservers.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverWithoutMiddlewares : IEventObserver<TestEvent>
        {
            private readonly TestObservations observations;

            public TestEventObserverWithoutMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.EventsFromObservers.Add(evt);
                observations.CancellationTokensFromObservers.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverWithRetryMiddleware : IEventObserver<TestEvent>
        {
            private readonly TestObservations observations;

            public TestEventObserverWithRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestEventObserverRetryMiddleware]
            [TestEventObserverMiddleware]
            [TestEventObserverMiddleware2]
            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.EventsFromObservers.Add(evt);
                observations.CancellationTokensFromObservers.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverWithMultipleMutatingMiddlewares : IEventObserver<TestEvent>
        {
            private readonly TestObservations observations;

            public TestEventObserverWithMultipleMutatingMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            [MutatingTestEventObserverMiddleware]
            [MutatingTestEventObserverMiddleware2]
            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.EventsFromObservers.Add(evt);
                observations.CancellationTokensFromObservers.Add(cancellationToken);
            }
        }

        private sealed class TestEventObserverMiddlewareAttribute : EventObserverMiddlewareConfigurationAttribute
        {
            public int Parameter { get; set; }
        }

        private sealed class TestEventObserverMiddleware2Attribute : EventObserverMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareAttribute>
        {
            private readonly TestObservations observations;

            public TestEventObserverMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareAttribute> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestEventObserverMiddleware2 : IEventObserverMiddleware<TestEventObserverMiddleware2Attribute>
        {
            private readonly TestObservations observations;

            public TestEventObserverMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddleware2Attribute> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestEventObserverRetryMiddlewareAttribute : EventObserverMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestEventObserverRetryMiddleware : IEventObserverMiddleware<TestEventObserverRetryMiddlewareAttribute>
        {
            private readonly TestObservations observations;

            public TestEventObserverRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverRetryMiddlewareAttribute> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class MutatingTestEventObserverMiddlewareAttribute : EventObserverMiddlewareConfigurationAttribute
        {
        }

        private sealed class MutatingTestEventObserverMiddleware2Attribute : EventObserverMiddlewareConfigurationAttribute
        {
        }

        private sealed class MutatingTestEventObserverMiddleware : IEventObserverMiddleware<MutatingTestEventObserverMiddlewareAttribute>
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestEventObserverMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, MutatingTestEventObserverMiddlewareAttribute> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                var testEvent = (TestEvent)(object)ctx.Event;
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 4 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[3]);
            }
        }

        private sealed class MutatingTestEventObserverMiddleware2 : IEventObserverMiddleware<MutatingTestEventObserverMiddleware2Attribute>
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestEventObserverMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, MutatingTestEventObserverMiddleware2Attribute> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                var testEvent = (TestEvent)(object)ctx.Event;
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 8 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[4]);
            }
        }

        private sealed class TestEventPublisherMiddleware : IEventPublisherMiddleware
        {
            private readonly TestObservations observations;

            public TestEventPublisherMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestEventPublisherMiddleware2 : IEventPublisherMiddleware
        {
            private readonly TestObservations observations;

            public TestEventPublisherMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestEventPublisherRetryMiddleware : IEventPublisherMiddleware
        {
            private readonly TestObservations observations;

            public TestEventPublisherRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class MutatingTestEventPublisherMiddleware : IEventPublisherMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestEventPublisherMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                var testEvent = (TestEvent)(object)ctx.Event;
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 1 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[1]);
            }
        }

        private sealed class MutatingTestEventPublisherMiddleware2 : IEventPublisherMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestEventPublisherMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.EventsFromMiddlewares.Add(ctx.Event);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                var testEvent = (TestEvent)(object)ctx.Event;
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 2 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[2]);
            }
        }

        private sealed class TestEventObserverMiddlewareWithMultipleInterfaces : IEventObserverMiddleware<TestEventObserverMiddlewareAttribute>,
                                                                                 IEventObserverMiddleware<TestEventObserverMiddleware2Attribute>
        {
            public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddleware2Attribute> ctx)
                where TEvent : class =>
                throw new InvalidOperationException("this middleware should never be called");

            public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareAttribute> ctx)
                where TEvent : class =>
                throw new InvalidOperationException("this middleware should never be called");
        }

        private sealed class TestEventObserverMiddlewareWithoutInterfaces : IEventObserverMiddleware
        {
        }

        private sealed class TestObservations
        {
            public List<Type> MiddlewareTypes { get; } = new();

            public List<object> EventsFromObservers { get; } = new();

            public List<object> EventsFromMiddlewares { get; } = new();

            public List<CancellationToken> CancellationTokensFromObservers { get; } = new();

            public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

            public List<EventObserverMiddlewareConfigurationAttribute> AttributesFromMiddlewares { get; } = new();
        }

        private sealed class CancellationTokensToUse
        {
            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
