namespace Conqueror.Eventing.Tests;

public sealed class EventObserverMiddlewareRegistrationTests
{
    [Test]
    public void GivenAlreadyRegisteredPlainMiddleware_RegisteringSameMiddlewareAsPlainDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventObserverMiddleware)), Is.EqualTo(1));
    }
    
    [Test]
    public void GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringSameMiddlewareAsPlainDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventObserverMiddlewareWithConfiguration)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddleware_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddleware_RegisteringSameMiddlewareAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserverMiddleware(observations);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringSameMiddlewareAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserverMiddlewareWithConfiguration(observations);

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>()
                    .AddConquerorEventObserverMiddleware(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddleware_RegisteringSameMiddlewareWithDifferentLifetimeOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringSameMiddlewareWithDifferentLifetimeOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithFactory_RegisteringSameMiddlewareAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationWithFactory_RegisteringSameMiddlewareAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithFactory_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalFactoryWasCalled = false;
        var newFactoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ =>
                    {
                        newFactoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(originalFactoryWasCalled, Is.False);
        Assert.That(newFactoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationWithFactory_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalFactoryWasCalled = false;
        var newFactoryWasCalled = false;

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ =>
                    {
                        newFactoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(originalFactoryWasCalled, Is.False);
        Assert.That(newFactoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithFactory_RegisteringSameMiddlewareAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserverMiddleware(observations);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserverMiddleware(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationWithFactory_RegisteringSameMiddlewareAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserverMiddlewareWithConfiguration(observations);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserverMiddleware(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithFactory_RegisteringSameMiddlewareWithDifferentLifetimeOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ => new(observations))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ => new(observations), ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationWithFactory_RegisteringSameMiddlewareWithDifferentLifetimeOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ => new(observations))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ => new(observations), ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareSingleton_RegisteringSameMiddlewareAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventObserverMiddleware(observations1);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware(singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton(observations2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationSingleton_RegisteringSameMiddlewareAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventObserverMiddlewareWithConfiguration(observations1);

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware(singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>()
                    .AddSingleton(observations2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareSingleton_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventObserverMiddleware(observations1);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware(singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations2);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationSingleton_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventObserverMiddlewareWithConfiguration(observations1);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware(singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations2);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareSingleton_RegisteringSameMiddlewareAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton1 = new TestEventObserverMiddleware(observations1);
        var singleton2 = new TestEventObserverMiddleware(observations2);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware(singleton1)
                    .AddConquerorEventObserverMiddleware(singleton2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationSingleton_RegisteringSameMiddlewareAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton1 = new TestEventObserverMiddlewareWithConfiguration(observations1);
        var singleton2 = new TestEventObserverMiddlewareWithConfiguration(observations2);

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, 
                                                                  configurePipeline: p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware(singleton1)
                    .AddConquerorEventObserverMiddleware(singleton2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    private sealed record TestEvent;

    private sealed class TestEventObserver : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline) =>
            pipeline.Use<TestEventObserverMiddleware>();
    }

    private sealed class TestEventObserverMiddleware : IEventObserverMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed record TestEventObserverMiddlewareConfiguration;

    private sealed class TestEventObserverMiddlewareWithConfiguration : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverMiddlewareWithConfiguration(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = new();
    }
}
