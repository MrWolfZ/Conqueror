namespace Conqueror.Eventing.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "order makes sense, but some types must be private to not interfere with assembly scanning")]
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
    public void GivenAlreadyRegisteredPlainMiddleware_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareForAssemblyScanning>()
                    .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventObserverMiddlewareForAssemblyScanning)), Is.EqualTo(1));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainMiddlewareWithConfiguration_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfigurationForAssemblyScanning>()
                    .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventObserverMiddlewareWithConfigurationForAssemblyScanning)), Is.EqualTo(1));
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ => new(observations))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfiguration>(_ => new(observations), ServiceLifetime.Singleton);

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

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask,
                                                                  p => p.Use<TestEventObserverMiddlewareForAssemblyScanning>())
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareForAssemblyScanning>(_ =>
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

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask,
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfigurationForAssemblyScanning, TestEventObserverMiddlewareConfigurationForAssemblyScanning>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithConfigurationForAssemblyScanning>(_ =>
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
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
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfiguration, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware(singleton1)
                    .AddConquerorEventObserverMiddleware(singleton2);

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
        var singleton = new TestEventObserverMiddlewareForAssemblyScanning();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask,
                                                                  p => p.Use<TestEventObserverMiddlewareForAssemblyScanning>())
                    .AddConquerorEventObserverMiddleware(singleton)
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
        var singleton = new TestEventObserverMiddlewareWithConfigurationForAssemblyScanning();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask,
                                                                  p => p.Use<TestEventObserverMiddlewareWithConfigurationForAssemblyScanning, TestEventObserverMiddlewareConfigurationForAssemblyScanning>(new()))
                    .AddConquerorEventObserverMiddleware(singleton)
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(singleton.InvocationCount, Is.EqualTo(1));
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

    private sealed class TestEventObserverMiddleware(TestObservations observations) : IEventObserverMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed record TestEventObserverMiddlewareConfiguration;

    private sealed class TestEventObserverMiddlewareWithConfiguration(TestObservations observations) : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    public sealed class TestEventObserverMiddlewareForAssemblyScanning : IEventObserverMiddleware
    {
        public int InvocationCount { get; private set; }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            InvocationCount += 1;
            await Task.Yield();
        }
    }

    public sealed record TestEventObserverMiddlewareConfigurationForAssemblyScanning;

    public sealed class TestEventObserverMiddlewareWithConfigurationForAssemblyScanning : IEventObserverMiddleware<TestEventObserverMiddlewareConfigurationForAssemblyScanning>
    {
        public int InvocationCount { get; private set; }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfigurationForAssemblyScanning> ctx)
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
