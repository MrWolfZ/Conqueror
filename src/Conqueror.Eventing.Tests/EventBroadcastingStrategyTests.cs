using System.Collections.Concurrent;

namespace Conqueror.Eventing.Tests;

public sealed class EventBroadcastingStrategyTests
{
    [Test]
    public async Task GivenCustomBroadcastingStrategy_CustomStrategyIsUsedWhenPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions.Select(t => (t.StrategyType, t.Event)), Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), evt) }));
        Assert.That(observations.ObservedStrategyExecutions.Select(t => t.StrategyInstance).Distinct().Count(), Is.EqualTo(1));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions.Select(t => (t.StrategyType, t.Event)), Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), evt),
            (typeof(TestBroadcastingStrategy), evt),
        }));
        Assert.That(observations.ObservedStrategyExecutions.Select(t => t.StrategyInstance).Distinct().Count(), Is.EqualTo(2));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public async Task GivenScopedCustomBroadcastingStrategy_CustomStrategyFromDispatchingScopeIsUsedWhenPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var strategyInstance1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventBroadcastingStrategy>();

        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var strategyInstance2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventBroadcastingStrategy>();

        var evt = new TestEvent { Payload = 10 };

        await observer1.HandleEvent(evt);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), strategyInstance1, evt) }));

        await observer2.HandleEvent(evt);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), strategyInstance1, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance2, evt),
        }));

        await dispatcher1.DispatchEvent(evt);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), strategyInstance1, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance2, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance1, evt),
        }));

        await dispatcher2.DispatchEvent(evt);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), strategyInstance1, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance2, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance1, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance2, evt),
        }));
    }

    [Test]
    public async Task GivenCustomBroadcastingStrategySingleton_CustomStrategyIsUsedWhenPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy<TestBroadcastingStrategy>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();
        var strategyInstance = provider.GetRequiredService<IConquerorEventBroadcastingStrategy>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), strategyInstance, evt) }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), strategyInstance, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance, evt),
        }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public async Task GivenCustomBroadcastingStrategyRegisteredWithFactory_CustomStrategyIsUsedWhenPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy(p => new TestBroadcastingStrategy(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();
        var strategyInstance = provider.GetRequiredService<IConquerorEventBroadcastingStrategy>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), strategyInstance, evt) }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), strategyInstance, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance, evt),
        }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public async Task GivenCustomBroadcastingStrategyRegisteredAsSingleton_CustomStrategyIsUsedWhenPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy(new TestBroadcastingStrategy(observations))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();
        var strategyInstance = provider.GetRequiredService<IConquerorEventBroadcastingStrategy>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), strategyInstance, evt) }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[]
        {
            (typeof(TestBroadcastingStrategy), strategyInstance, evt),
            (typeof(TestBroadcastingStrategy), strategyInstance, evt),
        }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public void GivenCustomBroadcastingStrategyThatThrows_SameExceptionIsRethrownFromPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventBroadcastingStrategy(new TestBroadcastingStrategy(observations, exception))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenNoExplicitBroadcastingStrategy_SequentialStrategyIsUsed()
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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var executionTask1 = observer.HandleEvent(evt);

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

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
                    .AddSequentialConquerorEventBroadcastingStrategy(c => c.ExceptionHandling = SequentialEventBroadcastingStrategyExceptionHandling.ThrowOnFirstException)
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
        }));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

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
                    .AddSequentialConquerorEventBroadcastingStrategy(c => c.ExceptionHandling = SequentialEventBroadcastingStrategyExceptionHandling.ThrowAfterAll)
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

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
                    .AddSequentialConquerorEventBroadcastingStrategy(c => c.ExceptionHandling = SequentialEventBroadcastingStrategyExceptionHandling.ThrowAfterAll)
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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<AggregateException>(() => observer.HandleEvent(evt));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        thrownException = Assert.ThrowsAsync<AggregateException>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception1, exception2 }));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        Assert.DoesNotThrowAsync(() => observer.HandleEvent(evt, cts.Token));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => observer.HandleEvent(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchEvent(evt, cts.Token));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => observer.HandleEvent(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchEvent(evt, cts.Token));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<AggregateException>(() => observer.HandleEvent(evt, cts.Token));

        Assert.That(thrownException?.InnerExceptions, Has.Exactly(1).SameAs(exception).And.Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        thrownException = Assert.ThrowsAsync<AggregateException>(() => dispatcher.DispatchEvent(evt, cts.Token));

        Assert.That(thrownException?.InnerExceptions, Has.Exactly(1).SameAs(exception).And.Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenConfiguredParallelBroadcastingStrategy_ParallelStrategyIsUsed()
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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var executionTask1 = observer.HandleEvent(evt);

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<AggregateException>(() => observer.HandleEvent(evt));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception }));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        thrownException = Assert.ThrowsAsync<AggregateException>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception }));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<AggregateException>(() => observer.HandleEvent(evt));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        thrownException = Assert.ThrowsAsync<AggregateException>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception1, exception2 }));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        Assert.DoesNotThrowAsync(() => observer.HandleEvent(evt, cts.Token));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => observer.HandleEvent(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.End),
        }));

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchEvent(evt, cts.Token));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => observer.HandleEvent(evt, cts.Token));

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchEvent(evt, cts.Token));

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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        // dispatch a noop event to initialize the in-memory publisher, otherwise the publisher would throw due
        // to the token being cancelled
        await dispatcher.DispatchEvent(new object());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<AggregateException>(() => observer.HandleEvent(evt, cts.Token));

        Assert.That(thrownException?.InnerExceptions, Has.Exactly(1).SameAs(exception).And.Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));

        thrownException = Assert.ThrowsAsync<AggregateException>(() => dispatcher.DispatchEvent(evt, cts.Token));

        Assert.That(thrownException?.InnerExceptions, Has.Exactly(1).SameAs(exception).And.Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedObserverExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),

            (typeof(TestEventObserver), evt, ObserverExecutionPhase.Start),
            (typeof(TestEventObserver2), evt, ObserverExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategyWithNonNegativeDegreeOfParallelism_ParallelStrategyIsUsedWithConfiguredParallelism()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        var tcs3 = new TaskCompletionSource();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddConquerorEventObserver<TestEventObserver3>()
                    .AddParallelConquerorEventBroadcastingStrategy(c => c.MaxDegreeOfParallelism = 2)
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventObserver, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventObserver2, CancellationToken, Task>>((_, _) => tcs2.Task);
        _ = services.AddSingleton<Func<TestEventObserver3, CancellationToken, Task>>((_, _) => tcs3.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var executionTask1 = observer.HandleEvent(evt);

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
    public void GivenParallelBroadcastingStrategyWithNegativeDegreeOfParallelism_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddParallelConquerorEventBroadcastingStrategy(c => c.MaxDegreeOfParallelism = -1));
    }

    [Test]
    public void GivenParallelBroadcastingStrategyWithZeroDegreeOfParallelism_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddParallelConquerorEventBroadcastingStrategy(c => c.MaxDegreeOfParallelism = 0));
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
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
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
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
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
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedObserverExecutions.Enqueue((GetType(), evt, ObserverExecutionPhase.End));
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
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
        Exception? exceptionToThrow = null)
        : IConquerorEventBroadcastingStrategy
    {
        public async Task BroadcastEvent<TEvent>(IReadOnlyCollection<IEventObserver<TEvent>> eventObservers, TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            observations.ObservedStrategyExecutions.Enqueue((GetType(), this, evt));
            observations.CancellationTokensFromCustomStrategy.Enqueue(cancellationToken);

            foreach (var observer in eventObservers)
            {
                await observer.HandleEvent(evt, cancellationToken);
            }

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<(Type StrategyType, object StrategyInstance, object Event)> ObservedStrategyExecutions { get; } = new();
        public ConcurrentQueue<CancellationToken> CancellationTokensFromCustomStrategy { get; } = new();
        public ConcurrentQueue<(Type ObserverType, object Event, ObserverExecutionPhase Phase)> ObservedObserverExecutions { get; } = new();
    }

    private enum ObserverExecutionPhase
    {
        Start,
        End,
    }
}
