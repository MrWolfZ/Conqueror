namespace Conqueror.Eventing.Tests.Publishing;

public sealed class EventTransportPublisherRegistrationTests
{
    [Test]
    public void GivenAlreadyRegisteredPlainPublisher_RegisteringSamePublisherAsPlainDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventTransportPublisher)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainPublisher_RegisteringSamePublisherWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainPublisher_RegisteringSamePublisherAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventTransportPublisher(observations);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPlainPublisher_RegisteringSamePublisherWithDifferentPipelineConfigurationOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalPipelineWasCalled = false;
        var newPipelineWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: _ => originalPipelineWasCalled = true)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: _ => newPipelineWasCalled = true)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(originalPipelineWasCalled, Is.False);
        Assert.That(newPipelineWasCalled, Is.True);
    }

    [Test]
    public void GivenAlreadyRegisteredPlainPublisher_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisherForAssemblyScanning>()
                    .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventTransportPublisherForAssemblyScanning)), Is.EqualTo(1));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherWithFactory_RegisteringSamePublisherAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherWithFactory_RegisteringSamePublisherWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalFactoryWasCalled = false;
        var newFactoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ =>
                    {
                        originalFactoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ =>
                    {
                        newFactoryWasCalled = true;
                        return new(observations);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(originalFactoryWasCalled, Is.False);
        Assert.That(newFactoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherWithFactory_RegisteringSamePublisherAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventTransportPublisher(observations);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations);
                    })
                    .AddConquerorEventTransportPublisher(singleton);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(factoryWasCalled, Is.False);
        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherWithFactory_RegisteringSamePublisherWithDifferentPipelineConfigurationOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var originalPipelineWasCalled = false;
        var newPipelineWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ => new(observations), configurePipeline: _ => originalPipelineWasCalled = true)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ => new(observations), configurePipeline: _ => newPipelineWasCalled = true)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(originalPipelineWasCalled, Is.False);
        Assert.That(newPipelineWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherWithFactory_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var factoryWasCalled = false;

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisherForAssemblyScanning>(_ =>
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
    public async Task GivenAlreadyRegisteredPublisherSingleton_RegisteringSamePublisherAsPlainOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventTransportPublisher(observations1);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(singleton)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherSingleton_RegisteringSamePublisherWithFactoryOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton = new TestEventTransportPublisher(observations1);
        var factoryWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(singleton)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(_ =>
                    {
                        factoryWasCalled = true;
                        return new(observations2);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
        Assert.That(factoryWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherSingleton_RegisteringSamePublisherAsInstanceOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations1 = new TestObservations();
        var observations2 = new TestObservations();
        var singleton1 = new TestEventTransportPublisher(observations1);
        var singleton2 = new TestEventTransportPublisher(observations2);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(singleton1)
                    .AddConquerorEventTransportPublisher(singleton2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(observations1.InvocationCounts, Is.Empty);
        Assert.That(observations2.InvocationCounts, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherSingleton_RegisteringSamePublisherWithDifferentPipelineConfigurationOverwritesRegistration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var singleton = new TestEventTransportPublisher(observations);
        var originalPipelineWasCalled = false;
        var newPipelineWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(singleton, _ => originalPipelineWasCalled = true)
                    .AddConquerorEventTransportPublisher(singleton, _ => newPipelineWasCalled = true);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        await observer.HandleEvent(new());

        Assert.That(originalPipelineWasCalled, Is.False);
        Assert.That(newPipelineWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAlreadyRegisteredPublisherSingleton_RegisteringViaAssemblyScanningDoesNothing()
    {
        var services = new ServiceCollection();
        var singleton = new TestEventTransportPublisherForAssemblyScanning();

        _ = services.AddConquerorEventTransportPublisher(singleton)
                    .AddConquerorEventingTypesFromExecutingAssembly();

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventForAssemblyScanning>>();

        await observer.HandleEvent(new());

        Assert.That(singleton.InvocationCount, Is.EqualTo(1));
    }

    [Test]
    public void GivenPublisherWithInvalidInterface_RegisteringPublisherThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher(new TestEventTransportPublisherWithoutValidInterfaces()));

        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher(new TestEventTransportPublisherWithMultipleInterfaces()));
    }

    [TestEventTransport(Parameter = 10)]
    private sealed record TestEventWithCustomPublisher;

    [TestEventTransportForAssemblyScanning]
    private sealed record TestEventForAssemblyScanning;

    private sealed class TestEventObserver : IEventObserver<TestEventWithCustomPublisher>
    {
        public async Task HandleEvent(TestEventWithCustomPublisher evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransport2Attribute : Attribute, IConquerorEventTransportConfigurationAttribute;

    private sealed class TestEventTransportPublisher(TestObservations observations) : IConquerorEventTransportPublisher<TestEventTransportAttribute>
    {
        private int invocationCount;

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            Assert.That(configurationAttribute.Parameter, Is.EqualTo(10));
        }
    }

    private sealed class TestEventTransportPublisherWithoutValidInterfaces : IConquerorEventTransportPublisher;

    private sealed class TestEventTransportPublisherWithMultipleInterfaces : IConquerorEventTransportPublisher<TestEventTransportAttribute>, IConquerorEventTransportPublisher<TestEventTransport2Attribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();
        }

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransport2Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransportForAssemblyScanningAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;

    public sealed class TestEventTransportPublisherForAssemblyScanning : IConquerorEventTransportPublisher<TestEventTransportForAssemblyScanningAttribute>
    {
        public int InvocationCount { get; private set; }

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportForAssemblyScanningAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
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
