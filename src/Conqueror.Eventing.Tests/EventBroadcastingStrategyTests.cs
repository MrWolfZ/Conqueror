using System.Collections.Concurrent;

namespace Conqueror.Eventing.Tests;

public sealed class EventBroadcastingStrategyTests
{
    [Test]
    public async Task GivenCustomBroadcastingStrategy_WhenPublishingEvent_CustomStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent { Payload = 10 };

        await observer.Handle(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), evt) }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), evt),
            (typeof(TestBroadcastingStrategy), evt),
        }));

        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public async Task GivenCustomBroadcastingStrategy_WhenPublishingEvent_StrategyIsResolvedFromPublishingScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IEventDispatcher>();

        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer1.Handle(evt);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[] { scope1.ServiceProvider }));
        Assert.That(observations.ServiceProvidersFromInstance, Is.EqualTo(new[] { scope1.ServiceProvider }));

        await observer2.Handle(evt);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
        }));

        Assert.That(observations.ServiceProvidersFromInstance, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
        }));

        await dispatcher1.DispatchEvent(evt);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
            scope1.ServiceProvider,
        }));

        Assert.That(observations.ServiceProvidersFromInstance, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
            scope1.ServiceProvider,
        }));

        await dispatcher2.DispatchEvent(evt);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
            scope1.ServiceProvider,
            scope2.ServiceProvider,
        }));

        Assert.That(observations.ServiceProvidersFromInstance, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
            scope1.ServiceProvider,
            scope2.ServiceProvider,
        }));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredStrategy_WhenRegisteringSameStrategyDifferently_OverwritesRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        Func<IServiceProvider, TestBroadcastingStrategy> factory = p => new(observations, p);
        var instance = new TestBroadcastingStrategy(observations, null!);

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>(),
                (null, "factory") => services.AddConquerorEventBroadcastingStrategy(factory),
                (var l, "type") => services.AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>(l.Value),
                (var l, "factory") => services.AddConquerorEventBroadcastingStrategy(factory, l.Value),
                (_, "instance") => services.AddConquerorEventBroadcastingStrategy(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestBroadcastingStrategy)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventBroadcastingStrategy)), Is.EqualTo(1));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).ImplementationType, Is.EqualTo(typeof(TestBroadcastingStrategy)));
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationType, Is.EqualTo(typeof(TestBroadcastingStrategy)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).ImplementationFactory, Is.Not.Null);
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationFactory, Is.Not.Null);
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).ImplementationInstance, Is.SameAs(instance));
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredStrategy_WhenRegisteringDifferentStrategy_ReplacesDefaultStrategyRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? firstLifetime,
        [Values("type", "factory", "instance")]
        string firstRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? secondLifetime,
        [Values("type", "factory", "instance")]
        string secondRegistrationMethod)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        Func<IServiceProvider, TestBroadcastingStrategy> factory = p => new(observations, p);
        Func<IServiceProvider, TestBroadcastingStrategy2> duplicateFactory = _ => new();
        var instance = new TestBroadcastingStrategy(observations, null!);
        var duplicateInstance = new TestBroadcastingStrategy2();

        _ = (firstLifetime, firstRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>(),
            (null, "factory") => services.AddConquerorEventBroadcastingStrategy(factory),
            (var l, "type") => services.AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>(l.Value),
            (var l, "factory") => services.AddConquerorEventBroadcastingStrategy(factory, l.Value),
            (_, "instance") => services.AddConquerorEventBroadcastingStrategy(instance),
            _ => throw new ArgumentOutOfRangeException(nameof(firstRegistrationMethod), firstRegistrationMethod, null),
        };

        _ = (secondLifetime, secondRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy2>(),
            (null, "factory") => services.AddConquerorEventBroadcastingStrategy(duplicateFactory),
            (var l, "type") => services.AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy2>(l.Value),
            (var l, "factory") => services.AddConquerorEventBroadcastingStrategy(duplicateFactory, l.Value),
            (_, "instance") => services.AddConquerorEventBroadcastingStrategy(duplicateInstance),
            _ => throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null),
        };

        Assert.That(services.Count(s => s.ServiceType == typeof(TestBroadcastingStrategy)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(TestBroadcastingStrategy2)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventBroadcastingStrategy)), Is.EqualTo(1));

        switch (firstLifetime, firstRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).ImplementationType, Is.EqualTo(typeof(TestBroadcastingStrategy)));
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationType, Is.Not.EqualTo(typeof(TestBroadcastingStrategy)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).ImplementationFactory, Is.Not.Null);

                if (secondRegistrationMethod != "factory")
                {
                    Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationFactory, Is.Null);
                }

                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy)).ImplementationInstance, Is.SameAs(instance));
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationInstance, Is.Not.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(firstRegistrationMethod), firstRegistrationMethod, null);
        }

        switch (secondLifetime, secondRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy2)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy2)).ImplementationType, Is.EqualTo(typeof(TestBroadcastingStrategy2)));
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationType, Is.EqualTo(typeof(TestBroadcastingStrategy2)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy2)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy2)).ImplementationFactory, Is.Not.Null);
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationFactory, Is.Not.Null);
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestBroadcastingStrategy2)).ImplementationInstance, Is.SameAs(duplicateInstance));
                Assert.That(services.Single(s => s.ServiceType == typeof(IEventBroadcastingStrategy)).ImplementationInstance, Is.SameAs(duplicateInstance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null);
        }
    }

    [Test]
    public void GivenCustomBroadcastingStrategy_WhenStrategyThrows_SameExceptionIsRethrownFromPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy(p => new TestBroadcastingStrategy(observations, p, exception))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.Exception.SameAs(exception));
        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenNoExplicitBroadcastingStrategy_WhenPublishing_SequentialStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>((_, _) => tcs2.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var executionTask1 = observer.Handle(evt);

        Assert.That(() => observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        tcs1 = new();
        tcs2 = new();

        var executionTask2 = dispatcher.DispatchEvent(evt);

        Assert.That(() => observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask2;

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithDefaultConfiguration_WhenObserverThrows_RethrowsExceptionImmediately()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }));

        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowOnFirstException_WhenObserverThrows_RethrowsExceptionImmediately()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy(c => c.WithThrowOnFirstException())
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }));

        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowAfterAll_WhenObserverThrows_RethrowsExceptionAfterAllObserversHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy(c => c.WithThrowAfterAll())
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowAfterAll_WhenMultipleObserversThrow_ThrowsAggregateExceptionAfterAllObserversHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy(c => c.WithThrowAfterAll())
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception1;
        });

        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception2;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.InstanceOf<AggregateException>()
                                                      .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.InstanceOf<AggregateException>()
                                                               .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndNoObserverThrows_CompletesExecutionWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        Assert.DoesNotThrowAsync(() => observer.Handle(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        Assert.DoesNotThrowAsync(() => dispatcher.DispatchEvent(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndOneObserverThrowsCancellationException_ThrowsCancellationExceptionAfterAllObserversHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        await Assert.ThatAsync(() => observer.Handle(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        await Assert.ThatAsync(() => dispatcher.DispatchEvent(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndMultipleObserversThrowCancellationException_ThrowsSingleCancellationExceptionAfterAllObserversHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        await Assert.ThatAsync(() => observer.Handle(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        await Assert.ThatAsync(() => dispatcher.DispatchEvent(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndSingleObserversThrowCancellationExceptionWhileOtherObserverThrowsOtherException_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSequentialConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        await Assert.ThatAsync(() => observer.Handle(evt, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                            .With.Property("InnerExceptions").Contains(exception)
                                                                            .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        await Assert.ThatAsync(() => dispatcher.DispatchEvent(evt, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                                     .With.Property("InnerExceptions").Contains(exception)
                                                                                     .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenConfiguredParallelBroadcastingStrategy_WhenPublishing_ParallelStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>((_, _) => tcs2.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var executionTask1 = observer.Handle(evt);

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
        }));

        tcs1 = new();
        tcs2 = new();

        var executionTask2 = dispatcher.DispatchEvent(evt);

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        await executionTask2;

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategy_WhenObserverThrows_ThrowsAggregateExceptionAfterAllObserversHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.InstanceOf<AggregateException>()
                                                      .With.Property("InnerExceptions").EquivalentTo(new[] { exception }));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.InstanceOf<AggregateException>()
                                                               .With.Property("InnerExceptions").EquivalentTo(new[] { exception }));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategy_WhenMultipleObserversThrow_ThrowsAggregateExceptionAfterAllObserversHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception1;
        });

        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception2;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        Assert.That(() => observer.Handle(evt), Throws.InstanceOf<AggregateException>()
                                                      .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        Assert.That(() => dispatcher.DispatchEvent(evt), Throws.InstanceOf<AggregateException>()
                                                               .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndNoObserverThrows_CompletesExecutionWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        Assert.DoesNotThrowAsync(() => observer.Handle(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        Assert.DoesNotThrowAsync(() => dispatcher.DispatchEvent(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndOneObserverThrowsCancellationException_ThrowsCancellationExceptionAfterAllObserversHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        await Assert.ThatAsync(() => observer.Handle(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        await Assert.ThatAsync(() => dispatcher.DispatchEvent(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndMultipleObserversThrowCancellationException_ThrowsSingleCancellationExceptionAfterAllObserversHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        await Assert.ThatAsync(() => observer.Handle(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        await Assert.ThatAsync(() => dispatcher.DispatchEvent(evt, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndSingleObserversThrowCancellationExceptionWhileOtherObserverThrowsOtherException_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddParallelConquerorEventBroadcastingStrategy()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        await Assert.ThatAsync(() => observer.Handle(evt, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                            .With.Property("InnerExceptions").Contains(exception)
                                                                            .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        await Assert.ThatAsync(() => dispatcher.DispatchEvent(evt, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                                     .With.Property("InnerExceptions").Contains(exception)
                                                                                     .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategyWithNonNegativeDegreeOfParallelism_WhenPublishingEvent_ParallelStrategyIsUsedWithConfiguredParallelism()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        var tcs3 = new TaskCompletionSource();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddConquerorEventObserver<TestEventObserver3>()
                    .AddParallelConquerorEventBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(2))
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>((_, _) => tcs2.Task);
        _ = services.AddSingleton<Func<TestEventObserver3, CancellationToken, Task>>((_, _) => tcs3.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var executionTask1 = observer.Handle(evt);

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs3.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.End),
        }));

        tcs1 = new();
        tcs2 = new();
        tcs3 = new();

        observations.ObservedObserverExecutions.Clear();

        var executionTask2 = dispatcher.DispatchEvent(evt);

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        Assert.That(() => observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs3.SetResult();

        await executionTask2;

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.End),
            (typeof(TestEventObserver3), evt, ObserverExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategyWithNegativeDegreeOfParallelism_WhenRegisteringStrategy_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddParallelConquerorEventBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(-1)));
    }

    [Test]
    public void GivenParallelBroadcastingStrategyWithZeroDegreeOfParallelism_WhenRegisteringStrategy_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddParallelConquerorEventBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(0)));
    }

    private sealed record TestEvent
    {
        public int Payload { get; init; }
    }

    private sealed record TestEvent2
    {
        public int Payload { get; init; }
    }

    private sealed class TestEventObserver(
        TestObservations observations,
        Func<TestEventObserver, CancellationToken, Task>? onEvent = null)
        : IEventObserver<TestEvent>,
          IEventObserver<TestEvent2>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }

        public async Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }
    }

    private sealed class TestEventObserver2(
        TestObservations observations,
        Func<TestEventObserver2, CancellationToken, Task>? onEvent = null)
        : IEventObserver<TestEvent>,
          IEventObserver<TestEvent2>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }

        public async Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }
    }

    private sealed class TestEventObserver3(
        TestObservations observations,
        Func<TestEventObserver3, CancellationToken, Task>? onEvent = null)
        : IEventObserver<TestEvent>,
          IEventObserver<TestEvent2>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }

        public async Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }
    }

    private sealed class TestBroadcastingStrategy(
        TestObservations observations,
        IServiceProvider serviceProviderFromInstance,
        Exception? exceptionToThrow = null)
        : IEventBroadcastingStrategy
    {
        public async Task BroadcastEvent(IReadOnlyCollection<EventObserverFn> eventObservers,
                                         IServiceProvider serviceProvider,
                                         object evt,
                                         CancellationToken cancellationToken)
        {
            observations.ObservedStrategyExecutions.Enqueue((GetType(), evt));
            observations.CancellationTokensFromCustomStrategy.Enqueue(cancellationToken);
            observations.ServiceProvidersFromInstance.Enqueue(serviceProviderFromInstance);
            observations.ServiceProvidersFromPublish.Enqueue(serviceProvider);

            foreach (var observer in eventObservers)
            {
                await observer(evt, cancellationToken);
            }

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestBroadcastingStrategy2 : IEventBroadcastingStrategy
    {
        public Task BroadcastEvent(IReadOnlyCollection<EventObserverFn> eventObservers,
                                   IServiceProvider serviceProvider,
                                   object evt,
                                   CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<(Type StrategyType, object Event)> ObservedStrategyExecutions { get; } = [];
        public ConcurrentQueue<CancellationToken> CancellationTokensFromCustomStrategy { get; } = [];
        public ConcurrentQueue<(Type ObserverType, object Event, ObserverExecutionPhase Phase)> ObservedObserverExecutions { get; } = [];
        public ConcurrentQueue<IServiceProvider> ServiceProvidersFromInstance { get; } = [];
        public ConcurrentQueue<IServiceProvider> ServiceProvidersFromPublish { get; } = [];
    }

    private enum ObserverExecutionPhase
    {
        Start,
        End,
    }
}
