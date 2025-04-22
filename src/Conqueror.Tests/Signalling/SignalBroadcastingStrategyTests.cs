using System.Collections.Concurrent;
using Conqueror.Signalling;

namespace Conqueror.Tests.Signalling;

public sealed partial class SignalBroadcastingStrategyTests
{
    [Test]
    public async Task GivenCustomBroadcastingStrategy_WhenPublishingSignal_CustomStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        using var cts = new CancellationTokenSource();

        var signal = new TestSignal { Payload = 10 };

        await handler.Handle(signal, cts.Token);

        Assert.That(observations.ObservedStrategyExecutions, Is.EqualTo(new[] { (typeof(TestBroadcastingStrategy), signal) }));
        Assert.That(observations.CancellationTokensFromCustomStrategy, Is.EqualTo(new[] { cts.Token }));
    }

    [Test]
    public async Task GivenCustomBroadcastingStrategy_WhenPublishingSignal_StrategyIsResolvedFromPublishingScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton<TestBroadcastingStrategy>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider
                             .GetRequiredService<ISignalPublishers>()
                             .For(TestSignal.T)
                             .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        var handler2 = scope2.ServiceProvider
                             .GetRequiredService<ISignalPublishers>()
                             .For(TestSignal.T)
                             .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        var signal = new TestSignal { Payload = 10 };

        await handler1.Handle(signal);

        Assert.That(observations.ServiceProvidersFromPublish, Is.EqualTo(new[] { scope1.ServiceProvider }));

        await handler2.Handle(signal);

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

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(_ => new TestBroadcastingStrategy(observations, exception))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcess(p.ServiceProvider.GetRequiredService<TestBroadcastingStrategy>()));

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenNoExplicitBroadcastingStrategy_WhenPublishing_SequentialStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>((_, _) => tcs2.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var signal = new TestSignal { Payload = 10 };

        var executionTask1 = handler.Handle(signal);

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithDefaultConfiguration_WhenHandlerThrows_RethrowsExceptionImmediately()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowOnFirstException_WhenHandlerThrows_RethrowsExceptionImmediately()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy(c => c.WithThrowOnFirstException()));

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowAfterAll_WhenHandlerThrows_RethrowsExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy(c => c.WithThrowAfterAll()));

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.Exception.SameAs(exception));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenSequentialBroadcastingStrategyWithThrowAfterAll_WhenMultipleHandlersThrow_ThrowsAggregateExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception1;
        });

        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception2;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy(c => c.WithThrowAfterAll()));

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.InstanceOf<AggregateException>()
                                                        .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndNoHandlerThrows_CompletesExecutionWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        Assert.DoesNotThrowAsync(() => handler.Handle(signal, cts.Token));

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndOneHandlerThrowsCancellationException_ThrowsCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndMultipleHandlersThrowCancellationException_ThrowsSingleCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenSequentialBroadcastingStrategy_WhenPublishIsCancelledAndSingleHandlersThrowCancellationExceptionWhileOtherHandlerThrowsOtherException_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithSequentialBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                              .With.Property("InnerExceptions").Contains(exception)
                                                                              .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EqualTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenConfiguredParallelBroadcastingStrategy_WhenPublishing_ParallelStrategyIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>((_, _) => tcs2.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        var signal = new TestSignal { Payload = 10 };

        var executionTask1 = handler.Handle(signal);

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategy_WhenHandlerThrows_ThrowsAggregateExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.InstanceOf<AggregateException>()
                                                        .With.Property("InnerExceptions").EquivalentTo(new[] { exception }));

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public void GivenParallelBroadcastingStrategy_WhenMultipleHandlersThrow_ThrowsAggregateExceptionAfterAllHandlersHaveFinished()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception1;
        });

        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception2;
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        var signal = new TestSignal { Payload = 10 };

        Assert.That(() => handler.Handle(signal), Throws.InstanceOf<AggregateException>()
                                                        .With.Property("InnerExceptions").EquivalentTo(new[] { exception1, exception2 }));

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndNoHandlerThrows_CompletesExecutionWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        Assert.DoesNotThrowAsync(() => handler.Handle(signal, cts.Token));

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndOneHandlerThrowsCancellationException_ThrowsCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndMultipleHandlersThrowCancellationException_ThrowsSingleCancellationExceptionAfterAllHandlersHaveExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal, cts.Token), Throws.InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategy_WhenPublishIsCancelledAndSingleHandlersThrowCancellationExceptionWhileOtherHandlerThrowsOtherException_ThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>(async (_, ct) =>
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        });

        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>(async (_, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        await WarmUpInProcessReceiver(provider);

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal, cts.Token), Throws.InstanceOf<AggregateException>()
                                                                              .With.Property("InnerExceptions").Contains(exception)
                                                                              .And.Property("InnerExceptions").Exactly(1).InstanceOf<OperationCanceledException>());

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }));
    }

    [Test]
    public async Task GivenParallelBroadcastingStrategyWithNonNegativeDegreeOfParallelism_WhenPublishingSignal_ParallelStrategyIsUsedWithConfiguredParallelism()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        var tcs3 = new TaskCompletionSource();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSignalHandler<TestSignalHandler2>()
                    .AddSignalHandler<TestSignalHandler3>()
                    .AddSingleton(observations);

        // ReSharper disable AccessToModifiedClosure (intentional)
        _ = services.AddSingleton<Func<TestSignalHandler, CancellationToken, Task>>((_, _) => tcs1.Task);
        _ = services.AddSingleton<Func<TestSignalHandler2, CancellationToken, Task>>((_, _) => tcs2.Task);
        _ = services.AddSingleton<Func<TestSignalHandler3, CancellationToken, Task>>((_, _) => tcs3.Task);
        //// ReSharper restore AccessToModifiedClosure

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(2)));

        var signal = new TestSignal { Payload = 10 };

        var executionTask1 = handler.Handle(signal);

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs2.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler3), signal, HandlerExecutionPhase.Start),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs1.SetResult();

        Assert.That(() => observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler3), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.End),
        }).After(100).MilliSeconds.PollEvery(10).MilliSeconds);

        tcs3.SetResult();

        await executionTask1;

        Assert.That(observations.ObservedHandlerExecutions, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler2), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler3), signal, HandlerExecutionPhase.Start),
            (typeof(TestSignalHandler), signal, HandlerExecutionPhase.End),
            (typeof(TestSignalHandler3), signal, HandlerExecutionPhase.End),
        }));
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    public async Task GivenParallelBroadcastingStrategyWithInvalidDegreeOfParallelism_WhenCallingHandler_ThrowsArgumentException(int maxDegreeOfParallelism)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPublisher(p => p.UseInProcessWithParallelBroadcastingStrategy(c => c.WithMaxDegreeOfParallelism(-1)));

        var signal = new TestSignal { Payload = 10 };

        await Assert.ThatAsync(() => handler.Handle(signal), Throws.ArgumentException);
    }

    private static async Task WarmUpInProcessReceiver(ServiceProvider provider)
    {
        // "warm up" the receiver so that it doesn't get canceled before the Signal is published with a canceled token
        await provider.GetRequiredService<InProcessSignalReceiver>().Broadcast(new TestSignal { Payload = 10 },
                                                                               provider,
                                                                               new NullBroadcastingStrategy(),
                                                                               "test",
                                                                               CancellationToken.None);
    }

    [Signal]
    private sealed partial record TestSignal
    {
        public int Payload { get; init; }
    }

    [Signal]
    private sealed partial record TestSignal2
    {
        public int Payload { get; init; }
    }

    private sealed class TestSignalHandler(
        TestObservations observations,
        Func<TestSignalHandler, CancellationToken, Task>? onSignal = null)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.Start));

            if (onSignal is not null)
            {
                await onSignal(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.End));
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.Start));

            if (onSignal is not null)
            {
                await onSignal(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.End));
        }
    }

    private sealed class TestSignalHandler2(
        TestObservations observations,
        Func<TestSignalHandler2, CancellationToken, Task>? onSignal = null)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.Start));

            if (onSignal is not null)
            {
                await onSignal(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.End));
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.Start));

            if (onSignal is not null)
            {
                await onSignal(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.End));
        }
    }

    private sealed class TestSignalHandler3(
        TestObservations observations,
        Func<TestSignalHandler3, CancellationToken, Task>? onSignal = null)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.Start));

            if (onSignal is not null)
            {
                await onSignal(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.End));
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.Start));

            if (onSignal is not null)
            {
                await onSignal(this, cancellationToken);
            }

            observations.ObservedHandlerExecutions.Enqueue((GetType(), signal, HandlerExecutionPhase.End));
        }
    }

    private sealed class TestBroadcastingStrategy(
        TestObservations observations,
        Exception? exceptionToThrow = null)
        : ISignalBroadcastingStrategy
    {
        public async Task BroadcastSignal<TSignal>(IReadOnlyCollection<ISignalReceiverHandlerInvoker> signalHandlerInvokers,
                                                   IServiceProvider serviceProvider,
                                                   TSignal signal,
                                                   string transportTypeName,
                                                   CancellationToken cancellationToken)
            where TSignal : class, ISignal<TSignal>
        {
            observations.ObservedStrategyExecutions.Enqueue((GetType(), signal));
            observations.CancellationTokensFromCustomStrategy.Enqueue(cancellationToken);
            observations.ServiceProvidersFromPublish.Enqueue(serviceProvider);

            foreach (var invoker in signalHandlerInvokers)
            {
                await invoker.Invoke(serviceProvider, signal, transportTypeName, cancellationToken);
            }

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class NullBroadcastingStrategy : ISignalBroadcastingStrategy
    {
        public Task BroadcastSignal<TSignal>(IReadOnlyCollection<ISignalReceiverHandlerInvoker> signalHandlerInvokers,
                                             IServiceProvider serviceProvider,
                                             TSignal signal,
                                             string transportTypeName,
                                             CancellationToken cancellationToken)
            where TSignal : class, ISignal<TSignal>
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<(Type StrategyType, object Signal)> ObservedStrategyExecutions { get; } = [];
        public ConcurrentQueue<CancellationToken> CancellationTokensFromCustomStrategy { get; } = [];
        public ConcurrentQueue<(Type HandlerType, object Signal, HandlerExecutionPhase Phase)> ObservedHandlerExecutions { get; } = [];
        public ConcurrentQueue<IServiceProvider> ServiceProvidersFromPublish { get; } = [];
    }

    private enum HandlerExecutionPhase
    {
        Start,
        End,
    }
}
