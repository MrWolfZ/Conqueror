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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseDefault(new TestBroadcastingStrategy1(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt, cts.Token);

        Assert.That(observations.EventsFromCustomStrategy, Is.EqualTo(new[] { evt }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.EventsFromCustomStrategy, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public void GivenCustomBroadcastingStrategyThatThrows_SameExceptionIsRethrownFromPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseDefault(new TestBroadcastingStrategy1(observations, exception)))
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
    public async Task GivenOverriddenDefaultBroadcastingStrategy_LastConfiguredDefaultIsUsedWhenPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseDefault(new TestBroadcastingStrategy1(observations))
                                                                                             .UseDefault(new TestBroadcastingStrategy2(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[] { typeof(TestBroadcastingStrategy2) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[] { typeof(TestBroadcastingStrategy2), typeof(TestBroadcastingStrategy2) }));
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
    public async Task GivenSequentialBroadcastingStrategyForEventType_SequentialStrategyIsUsedOnlyForThatEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseDefault(new TestBroadcastingStrategy1(observations))
                                                                                             .UseSequentialForEventType<TestEvent>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt1 = new TestEvent { Payload = 10 };
        var evt2 = new TestEvent2 { Payload = 10 };

        await observer1.HandleEvent(evt1);

        Assert.That(observations.ObservedStrategyTypes, Is.Empty);

        await observer2.HandleEvent(evt2);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
        }));

        await dispatcher.DispatchEvent(evt1);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
        }));

        await dispatcher.DispatchEvent(evt2);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
            typeof(TestBroadcastingStrategy1),
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault(o => o.ExceptionHandling = SequentialEventBroadcastingStrategyExceptionHandling.ThrowOnFirstException))
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault(o => o.ExceptionHandling = SequentialEventBroadcastingStrategyExceptionHandling.ThrowAfterAll))
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault(o => o.ExceptionHandling = SequentialEventBroadcastingStrategyExceptionHandling.ThrowAfterAll))
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseSequentialAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
    public async Task GivenParallelBroadcastingStrategyForEventType_ParallelStrategyIsUsedOnlyForThatEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseDefault(new TestBroadcastingStrategy1(observations))
                                                                                             .UseParallelForEventType<TestEvent>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt1 = new TestEvent { Payload = 10 };
        var evt2 = new TestEvent2 { Payload = 10 };

        await observer1.HandleEvent(evt1);

        Assert.That(observations.ObservedStrategyTypes, Is.Empty);

        await observer2.HandleEvent(evt2);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
        }));

        await dispatcher.DispatchEvent(evt1);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
        }));

        await dispatcher.DispatchEvent(evt2);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
            typeof(TestBroadcastingStrategy1),
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault())
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
    public async Task GivenConfiguredBroadcastingStrategyForEventType_StrategyIsUsedForThatEventTypeAndDefaultIsUsedForOtherEventTypes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseDefault(new TestBroadcastingStrategy1(observations))
                                                                                             .UseForEventType<TestEvent2>(new TestBroadcastingStrategy2(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt1 = new TestEvent { Payload = 10 };
        var evt2 = new TestEvent2 { Payload = 10 };

        await observer1.HandleEvent(evt1);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
        }));

        await observer2.HandleEvent(evt2);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
            typeof(TestBroadcastingStrategy2),
        }));

        await dispatcher.DispatchEvent(evt1);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
            typeof(TestBroadcastingStrategy2),
            typeof(TestBroadcastingStrategy1),
        }));

        await dispatcher.DispatchEvent(evt2);

        Assert.That(observations.ObservedStrategyTypes, Is.EqualTo(new[]
        {
            typeof(TestBroadcastingStrategy1),
            typeof(TestBroadcastingStrategy2),
            typeof(TestBroadcastingStrategy1),
            typeof(TestBroadcastingStrategy2),
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
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault(o => o.MaxDegreeOfParallelism = 2))
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
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault(o => o.MaxDegreeOfParallelism = -1))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        _ = Assert.ThrowsAsync<ArgumentException>(() => observer.HandleEvent(evt));

        _ = Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchEvent(evt));
    }

    [Test]
    public void GivenParallelBroadcastingStrategyWithZeroDegreeOfParallelism_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .ConfigureInProcessEventBroadcastingStrategy(builder => builder.UseParallelAsDefault(o => o.MaxDegreeOfParallelism = 0))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        _ = Assert.ThrowsAsync<ArgumentException>(() => observer.HandleEvent(evt));

        _ = Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchEvent(evt));
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

    private sealed class TestBroadcastingStrategy1(
        TestObservations observations,
        Exception? exceptionToThrow = null)
        : IConquerorEventBroadcastingStrategy
    {
        public async Task BroadcastEvent<TEvent>(IReadOnlyCollection<IEventObserver<TEvent>> eventObservers, TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            observations.ObservedStrategyTypes.Enqueue(GetType());
            observations.EventsFromCustomStrategy.Enqueue(evt);
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

    private sealed class TestBroadcastingStrategy2(TestObservations observations) : IConquerorEventBroadcastingStrategy
    {
        public async Task BroadcastEvent<TEvent>(IReadOnlyCollection<IEventObserver<TEvent>> eventObservers, TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            observations.ObservedStrategyTypes.Enqueue(GetType());
            observations.EventsFromCustomStrategy.Enqueue(evt);
            observations.CancellationTokensFromCustomStrategy.Enqueue(cancellationToken);

            foreach (var observer in eventObservers)
            {
                await observer.HandleEvent(evt, cancellationToken);
            }
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<Type> ObservedStrategyTypes { get; } = new();

        public ConcurrentQueue<object> EventsFromCustomStrategy { get; } = new();

        public ConcurrentQueue<CancellationToken> CancellationTokensFromCustomStrategy { get; } = new();

        public ConcurrentQueue<(Type ObserverType, object Event, ObserverExecutionPhase Phase)> ObservedObserverExecutions { get; } = new();
    }

    private enum ObserverExecutionPhase
    {
        Start,
        End,
    }
}
