using System.Collections.Concurrent;
using Conqueror.Eventing;

namespace Conqueror.Tests.Eventing;

public sealed partial class EventNotificationBroadcastingStrategyTests
{
    [Test]
    public async Task GivenCustomBroadcastingStrategy_WhenPublishingEvent_CustomStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        using var cts = new CancellationTokenSource();

        var notification = new TestEventNotification { Payload = 10 };

        await handler.Handle(notification, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), notification) }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));
    }

    [Test]
    public async Task GivenCustomBroadcastingStrategy_WhenPublishingEvent_StrategyIsResolvedFromPublishingScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider
                             .GetRequiredService<IEventNotificationPublishers>()
                             .For(TestEventNotification.T)
                             .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        var handler2 = scope2.ServiceProvider
                             .GetRequiredService<IEventNotificationPublishers>()
                             .For(TestEventNotification.T)
                             .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        var notification = new TestEventNotification { Payload = 10 };

        await handler1.Handle(notification);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[] { scope1.ServiceProvider }));

        await handler2.Handle(notification);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope2.ServiceProvider,
        }));
    }

    [Test]
    public void GivenCustomBroadcastingStrategy_WhenStrategyThrows_SameExceptionIsRethrownFromPublishing()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(_ => new TestBroadcastingStrategy(observations, exception))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenNoExplicitBroadcastingStrategy_WhenPublishing_SequentialStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>((_, _) => tcs2.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var notification = new TestEventNotification { Payload = 10 };

        var executionTask1 = handler.Handle(notification);

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithDefaultConfiguration_WhenHandlerThrows_RethrowsExceptionImmediately()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowOnFirstException_WhenHandlerThrows_RethrowsExceptionImmediately()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy(c => c.WithThrowOnFirstException()));

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowAfterAll_WhenHandlerThrows_RethrowsExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy(c => c.WithThrowAfterAll()));

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowAfterAll_WhenMultipleHandlersThrow_ThrowsAggregateExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception1;
        });

        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception2;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy(c => c.WithThrowAfterAll()));

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.InstanceOf<AggregateException>()
                                                              .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndNoHandlerThrows_CompletesExecutionWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        Assert.DoesNotThrowAsync(() => handler.Handle(notification, cts.Token));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndOneHandlerThrowsCancellationException_ThrowsCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndMultipleHandlersThrowCancellationException_ThrowsSingleCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndSingleHandlersThrowCancellationExceptionWhileOtherHandlerThrowsOtherException_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                                    .With.Property("InnerExceptions").Contains(exception)
                                                                                    .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenConfiguredParallelBroadcastingStrategy_WhenPublishing_ParallelStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>((_, _) => tcs2.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        var notification = new TestEventNotification { Payload = 10 };

        var executionTask1 = handler.Handle(notification);

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategy_WhenHandlerThrows_ThrowsAggregateExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.InstanceOf<AggregateException>()
                                                              .With.Property("InnerExceptions").EquivalentTo(new[] { exception }));

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategy_WhenMultipleHandlersThrow_ThrowsAggregateExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception1;
        });

        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception2;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        var notification = new TestEventNotification { Payload = 10 };

        Assert.That(() => handler.Handle(notification), Throws.InstanceOf<AggregateException>()
                                                              .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndNoHandlerThrows_CompletesExecutionWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        Assert.DoesNotThrowAsync(() => handler.Handle(notification, cts.Token));

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndOneHandlerThrowsCancellationException_ThrowsCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndMultipleHandlersThrowCancellationException_ThrowsSingleCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndSingleHandlersThrowCancellationExceptionWhileOtherHandlerThrowsOtherException_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                                    .With.Property("InnerExceptions").Contains(exception)
                                                                                    .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
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

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddEventNotificationHandler<TestEventNotificationHandler2>()
                    .AddEventNotificationHandler<TestEventNotificationHandler3>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestEventNotificationHandler, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestEventNotificationHandler2, CancellationToken, Task>>((_, _) => tcs2.Task);
        _ = services.AddSingleton<Func<TestEventNotificationHandler3, CancellationToken, Task>>((_, _) => tcs3.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(2)));

        var notification = new TestEventNotification { Payload = 10 };

        var executionTask1 = handler.Handle(notification);

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler3), notification, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler3), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs3.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler2), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler3), notification, HandlerExecutionPhase.Start),
            (typeof(TestEventNotificationHandler), notification, HandlerExecutionPhase.End),
            (typeof(TestEventNotificationHandler3), notification, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public async Task GivenParallelBroadcastingStrategyWithInvalidDegreeOfParallelism_WhenCallingHandler_ThrowsArgumentException(int maxDegreeOfParallelism)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(-1)));

        var notification = new TestEventNotification { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(notification), Throws.ArgumentException);
    }

    private static async Task WarmUpInProcessReceiver(ServiceProvider provider)
    {
        // "warm up" the receiver so that it doesn't get canceled before the event is published with a canceled token
        await provider.GetRequiredService<InProcessEventNotificationReceiver>().Broadcast(new TestEventNotification { Payload = 10 },
                                                                                          provider,
                                                                                          new NullBroadcastingStrategy(),
                                                                                          "test",
                                                                                          CancellationToken.None);
    }

    [EventNotification]
    private sealed partial record TestEventNotification
    {
        public int Payload { get; init; }
    }

    [EventNotification]
    private sealed partial record TestEventNotification2
    {
        public int Payload { get; init; }
    }

    private sealed class TestEventNotificationHandler(
        TestObservations observations,
        Func<TestEventNotificationHandler, CancellationToken, Task>? onEvent = null)
        : TestEventNotification.IHandler,
          TestEventNotification2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.End));
        }

        public async Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.End));
        }
    }

    private sealed class TestEventNotificationHandler2(
        TestObservations observations,
        Func<TestEventNotificationHandler2, CancellationToken, Task>? onEvent = null)
        : TestEventNotification.IHandler,
          TestEventNotification2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.End));
        }

        public async Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.End));
        }
    }

    private sealed class TestEventNotificationHandler3(
        TestObservations observations,
        Func<TestEventNotificationHandler3, CancellationToken, Task>? onEvent = null)
        : TestEventNotification.IHandler,
          TestEventNotification2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.End));
        }

        public async Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.Start));

            if (onEvent is not null)
            {
                await onEvent(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), notification, HandlerExecutionPhase.End));
        }
    }

    private sealed class TestBroadcastingStrategy(
        TestObservations observations,
        Exception? exceptionToThrow = null)
        : IEventNotificationBroadcastingStrategy
    {
        public async Task BroadcastEventNotification<TEventNotification>(IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> eventNotificationHandlerInvokers,
                                                                         IServiceProvider serviceProvider,
                                                                         TEventNotification notification,
                                                                         string transportTypeName,
                                                                         CancellationToken cancellationToken)
            where TEventNotification : class, IEventNotification<TEventNotification>
        {
            observations.ObservedStrategyExecutions.Enqueue((GetType(), notification));
            observations.CancellationTokensFromCustomStrategy.Enqueue(cancellationToken);
            observations.ServiceProvidersFromPublish.Enqueue(serviceProvider);

            foreach (var invoker in eventNotificationHandlerInvokers)
            {
                await invoker.Invoke(serviceProvider, notification, transportTypeName, cancellationToken);
            }

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class NullBroadcastingStrategy : IEventNotificationBroadcastingStrategy
    {
        public Task BroadcastEventNotification<TEventNotification>(IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> eventNotificationHandlerInvokers,
                                                                   IServiceProvider serviceProvider,
                                                                   TEventNotification notification,
                                                                   string transportTypeName,
                                                                   CancellationToken cancellationToken)
            where TEventNotification : class, IEventNotification<TEventNotification>
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<(Type StrategyType, object Event)> ObservedStrategyExecutions { get; } = [];
        public ConcurrentQueue<CancellationToken> CancellationTokensFromCustomStrategy { get; } = [];
        public ConcurrentQueue<(Type HandlerType, object Event, HandlerExecutionPhase Phase)> ObservedHandlerExecutions { get; } = [];
        public ConcurrentQueue<IServiceProvider> ServiceProvidersFromPublish { get; } = [];
    }

    private enum HandlerExecutionPhase
    {
        Start,
        End,
    }
}
