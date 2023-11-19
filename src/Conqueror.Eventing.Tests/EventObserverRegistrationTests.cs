namespace Conqueror.Eventing.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "order makes sense, but some types must be private to not interfere with assembly scanning")]
public sealed class EventObserverRegistrationTests
{
    [Test]
    public void GivenAlreadyRegisteredPlainObserver_RegisteringSameObserverAsPlainDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver>();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventObserver)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainObserver_RegisteringSameObserverWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver>(_ =>
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
    public async Task GivenAlreadyRegisteredPlainObserver_RegisteringSameObserverAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserver(observations);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainObserver_RegisteringSameObserverWithDifferentLifetimeOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainObserver_RegisteringSameObserverWithDifferentTransportOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var builderConfiguration1WasCalled = false;
        var builderConfiguration2WasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>(configureTransports: b =>
                    {
                        builderConfiguration1WasCalled = true;
                        _ = b.UseInMemory();
                    })
                    .AddConquerorEventObserver<TestEventObserver>(configureTransports: b =>
                    {
                        builderConfiguration2WasCalled = true;
                        _ = b.UseInMemory();
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(builderConfiguration1WasCalled, Is.False);
        Assert.That(builderConfiguration2WasCalled, Is.True);
    }

    [Test]
    public void GivenAlreadyRegisteredPlainObserver_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserver<TestEventObserverForAssemblyScanning>()
                    .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventObserverForAssemblyScanning)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverWithFactory_RegisteringSameObserverAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverWithFactory_RegisteringSameObserverWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalFactoryWasCalled = false;
        var newFactoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserver<TestEventObserver>(_ =>
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
    public async Task GivenAlreadyRegisteredObserverForMultipleEventTypesWithFactory_RegisteringSameObserverWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalFactoryWasCalled = false;
        var newFactoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleInterfaces>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserver<TestEventObserverWithMultipleInterfaces>(_ =>
                    {
                        newFactoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();

        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        Assert.That(originalFactoryWasCalled, Is.False);
        Assert.That(newFactoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverWithFactory_RegisteringSameObserverAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserver(observations);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventObserver(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverWithFactory_RegisteringSameObserverWithDifferentLifetimeOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(_ => new(observations))
                    .AddConquerorEventObserver<TestEventObserver>(_ => new(observations), ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverWithFactory_RegisteringSameObserverWithDifferentTransportOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var builderConfiguration1WasCalled = false;
        var builderConfiguration2WasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>(_ => new(observations), configureTransports: b =>
                    {
                        builderConfiguration1WasCalled = true;
                        _ = b.UseInMemory();
                    })
                    .AddConquerorEventObserver<TestEventObserver>(_ => new(observations), configureTransports: b =>
                    {
                        builderConfiguration2WasCalled = true;
                        _ = b.UseInMemory();
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(builderConfiguration1WasCalled, Is.False);
        Assert.That(builderConfiguration2WasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverWithFactory_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserverForAssemblyScanning>(_ =>
                    {
                        factoryWasCalled = true;
                        return new();
                    })
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventForAssemblyScanning>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverSingleton_RegisteringSameObserverAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventObserver(observations1);

        _ = services.AddConquerorEventObserver(singleton)
                    .AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverSingleton_RegisteringSameObserverWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventObserver(observations1);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver(singleton)
                    .AddConquerorEventObserver<TestEventObserver>(_ =>
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
    public async Task GivenAlreadyRegisteredObserverSingleton_RegisteringSameObserverAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton1 = new TestEventObserver(observations1);
        var singleton2 = new TestEventObserver(observations2);

        _ = services.AddConquerorEventObserver(singleton1)
                    .AddConquerorEventObserver(singleton2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverSingleton_RegisteringSameObserverWithDifferentTransportOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventObserver(observations);
        var builderConfiguration1WasCalled = false;
        var builderConfiguration2WasCalled = false;

        _ = services.AddConquerorEventObserver(singleton, b =>
                    {
                        builderConfiguration1WasCalled = true;
                        _ = b.UseInMemory();
                    })
                    .AddConquerorEventObserver(singleton, b =>
                    {
                        builderConfiguration2WasCalled = true;
                        _ = b.UseInMemory();
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(builderConfiguration1WasCalled, Is.False);
        Assert.That(builderConfiguration2WasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredObserverSingleton_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var singleton = new TestEventObserverForAssemblyScanning();

        _ = services.AddConquerorEventObserver(singleton)
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventForAssemblyScanning>>();

        await observer.HandleEvent(new());

        Assert.That(singleton.InvocationCount, Is.EqualTo(1));
    }

    [Test]
    public void GivenValidObserverType_RegisteringAllEventingTypesViaAssemblyScanningRegistersObserver()
    {
        var provider = new ServiceCollection().AddConquerorEventingTypesFromExecutingAssembly()
                                              .BuildServiceProvider();

        Assert.That(() => provider.GetRequiredService<TestEventObserverForAssemblyScanning>(), Throws.Nothing);
    }

    [Test]
    public void GivenObserverWithInvalidInterface_RegisteringObserverThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserver<TestEventObserverWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserver<TestEventObserverWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserver(new TestEventObserverWithoutValidInterfaces()));
    }

    private sealed record TestEvent;

    private sealed record TestEvent2;

    public sealed record TestEventForAssemblyScanning;

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserver(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventObserverWithMultipleInterfaces : IEventObserver<TestEvent>, IEventObserver<TestEvent2>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventObserverWithoutValidInterfaces : IEventObserver
    {
    }

    public sealed class TestEventObserverForAssemblyScanning : IEventObserver<TestEventForAssemblyScanning>
    {
        public int InvocationCount { get; private set; }

        public async Task HandleEvent(TestEventForAssemblyScanning evt, CancellationToken cancellationToken = default)
        {
            InvocationCount += 1;
            await Task.Yield();
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = new();
    }
}
