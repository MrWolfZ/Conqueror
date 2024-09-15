namespace Conqueror.Eventing.Tests;

public sealed class EventPublisherMiddlewareRegistrationTests
{
    [Test]
    public void GivenAlreadyRegisteredPlainMiddleware_RegisteringSameMiddlewareAsPlainDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventPublisherMiddleware)), Is.EqualTo(1));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringSameMiddlewareAsPlainDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventPublisherMiddlewareWithConfiguration)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainMiddleware_RegisteringSameMiddlewareWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ =>
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ =>
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
        var singleton = new TestEventPublisherMiddleware(observations);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware(singleton);

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
        var singleton = new TestEventPublisherMiddlewareWithConfiguration(observations);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>()
                    .AddConquerorEventPublisherMiddleware(singleton);

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
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Singleton)
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainMiddleware_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareForAssemblyScanning>()
                    .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventPublisherMiddlewareForAssemblyScanning)), Is.EqualTo(1));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning>()
                    .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithFactory_RegisteringSameMiddlewareAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>()
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
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ =>
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ =>
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
        var singleton = new TestEventPublisherMiddleware(observations);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventPublisherMiddleware(singleton);

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
        var singleton = new TestEventPublisherMiddlewareWithConfiguration(observations);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventPublisherMiddleware(singleton);

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
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ => new(observations))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ => new(observations), ServiceLifetime.Singleton);

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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ => new(observations))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ => new(observations), ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithFactory_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareForAssemblyScanning>())
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareForAssemblyScanning>(_ =>
                    {
                        factoryWasCalled = true;
                        return new();
                    })
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationWithFactory_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning, TestEventPublisherMiddlewareConfigurationForAssemblyScanning>(new()))
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning>(_ =>
                    {
                        factoryWasCalled = true;
                        return new();
                    })
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareSingleton_RegisteringSameMiddlewareAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventPublisherMiddleware(observations1);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware(singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
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
        var singleton = new TestEventPublisherMiddlewareWithConfiguration(observations1);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware(singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>()
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
        var singleton = new TestEventPublisherMiddleware(observations1);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware(singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(_ =>
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
        var singleton = new TestEventPublisherMiddlewareWithConfiguration(observations1);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware(singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithConfiguration>(_ =>
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
        var singleton1 = new TestEventPublisherMiddleware(observations1);
        var singleton2 = new TestEventPublisherMiddleware(observations2);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
                    .AddConquerorEventPublisherMiddleware(singleton1)
                    .AddConquerorEventPublisherMiddleware(singleton2);

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
        var singleton1 = new TestEventPublisherMiddlewareWithConfiguration(observations1);
        var singleton2 = new TestEventPublisherMiddlewareWithConfiguration(observations2);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfiguration, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventPublisherMiddleware(singleton1)
                    .AddConquerorEventPublisherMiddleware(singleton2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareSingleton_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var singleton = new TestEventPublisherMiddlewareForAssemblyScanning();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareForAssemblyScanning>())
                    .AddConquerorEventPublisherMiddleware(singleton)
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(singleton.InvocationCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredMiddlewareWithConfigurationSingleton_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var singleton = new TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning, TestEventPublisherMiddlewareConfigurationForAssemblyScanning>(new()))
                    .AddConquerorEventPublisherMiddleware(singleton)
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(singleton.InvocationCount, Is.EqualTo(1));
    }

    private sealed record TestEvent;

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    private sealed class TestEventPublisherMiddleware(TestObservations observations) : IEventPublisherMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed record TestEventPublisherMiddlewareConfiguration;

    private sealed class TestEventPublisherMiddlewareWithConfiguration(TestObservations observations) : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    public sealed class TestEventPublisherMiddlewareForAssemblyScanning : IEventPublisherMiddleware
    {
        public int InvocationCount { get; private set; }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            InvocationCount += 1;
            await Task.Yield();
        }
    }

    public sealed record TestEventPublisherMiddlewareConfigurationForAssemblyScanning;

    public sealed class TestEventPublisherMiddlewareWithConfigurationForAssemblyScanning : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfigurationForAssemblyScanning>
    {
        public int InvocationCount { get; private set; }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfigurationForAssemblyScanning> ctx)
            where TEvent : class
        {
            InvocationCount += 1;
            await Task.Yield();
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = [];
    }
}
