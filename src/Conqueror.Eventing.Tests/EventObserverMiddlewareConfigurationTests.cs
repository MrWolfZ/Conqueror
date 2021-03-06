using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Eventing.Tests
{
    public sealed class EventObserverMiddlewareConfigurationTests
    {
        [Test]
        public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestEventObserverMiddlewareConfiguration(10);

            _ = services.ConfigureEventObserverPipeline<TestEventObserver>(pipeline =>
            {
                _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(initialConfiguration);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestEventObserverMiddlewareConfiguration(10);
            var overwrittenConfiguration = new TestEventObserverMiddlewareConfiguration(20);

            _ = services.ConfigureEventObserverPipeline<TestEventObserver>(pipeline =>
            {
                _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(overwrittenConfiguration);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestEventObserverMiddlewareConfiguration(10);

            _ = services.ConfigureEventObserverPipeline<TestEventObserver>(pipeline =>
            {
                _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(c => c.Parameter += 10);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

            Assert.AreEqual(20, initialConfiguration.Parameter);
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestEventObserverMiddlewareConfiguration(10);

            _ = services.ConfigureEventObserverPipeline<TestEventObserver>(pipeline =>
            {
                _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(c => new(c.Parameter + 10));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Has.Count.EqualTo(1));

            Assert.AreEqual(20, observations.Configurations[0].Parameter);
        }

        [Test]
        public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddSingleton(observations);

            _ = services.ConfigureEventObserverPipeline<TestEventObserver>(pipeline =>
            {
                _ = Assert.Throws<InvalidOperationException>(
                    () => pipeline.Configure<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new TestEventObserverMiddlewareConfiguration(20)));
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(c => c.Parameter += 10));
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(c => new(c.Parameter + 10)));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new(), CancellationToken.None);
        }

        [Test]
        public async Task GivenExternalPipelineConfigurationAndHandlerWithOwnPipelineConfiguration_OwnConfigurationIsUsed()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserverWithPipelineConfiguration>()
                        .AddTransient<TestEventObserverMiddleware>()
                        .AddSingleton(observations);

            _ = services.ConfigureEventObserverPipeline<TestEventObserverWithPipelineConfiguration>(pipeline =>
            {
                _ = Assert.Throws<InvalidOperationException>(
                    () => pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(
                        new(20)));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Has.Count.EqualTo(1));

            Assert.AreEqual(10, observations.Configurations[0].Parameter);
        }

        private sealed record TestEvent;

        private sealed class TestEventObserver : IEventObserver<TestEvent>
        {
            public async Task HandleEvent(TestEvent query, CancellationToken cancellationToken)
            {
                await Task.Yield();
            }
        }

        private sealed class TestEventObserverWithPipelineConfiguration : IEventObserver<TestEvent>
        {
            public async Task HandleEvent(TestEvent query, CancellationToken cancellationToken)
            {
                await Task.Yield();
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new(10));
            }
        }

        private sealed class TestEventObserverMiddlewareConfiguration
        {
            public TestEventObserverMiddlewareConfiguration(int parameter)
            {
                Parameter = parameter;
            }

            public int Parameter { get; set; }
        }

        private sealed class TestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
        {
            private readonly TestObservations observations;

            public TestEventObserverMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
                where TEvent : class
            {
                await Task.Yield();
                observations.Configurations.Add(ctx.Configuration);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<TestEventObserverMiddlewareConfiguration> Configurations { get; } = new();
        }
    }
}
