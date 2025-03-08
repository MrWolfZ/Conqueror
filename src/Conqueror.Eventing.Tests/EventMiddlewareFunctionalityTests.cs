using System.Collections.Concurrent;

namespace Conqueror.Eventing.Tests;

public sealed class EventMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenPublishAndObserverPipelines_WhenObserverIsCalled_MiddlewaresAreCalledWithEvent(ConquerorMiddlewareFunctionalityTestCase testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, pipeline =>
                    {
                        if (testCase.ConfigureObserver2Pipeline is null)
                        {
                            return;
                        }

                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

                        testCase.ConfigureObserver2Pipeline.Invoke(pipeline);
                    })
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
                    {
                        if (testCase.ConfigureObserver1Pipeline is null)
                        {
                            return;
                        }

                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

                        testCase.ConfigureObserver1Pipeline.Invoke(pipeline);
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var expectedMiddlewareTypes = testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType).ToList();
        var expectedTransportTypes = testCase.ExpectedMiddlewareTypes.Select(t => new EventTransportType(InProcessEventAttribute.TransportName, t.TransportRole)).ToList();
        var expectedTransportTypesFromPipelineBuilders = testCase.ExpectedTransportRolesFromPipelineBuilders.Select(r => new EventTransportType(InProcessEventAttribute.TransportName, r)).ToList();

        using var tokenSource = new CancellationTokenSource();

        var evt = new TestEvent(10);

        await observer.WithPipeline(pipeline =>
        {
            if (testCase.ConfigurePublishPipeline is null)
            {
                return;
            }

            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

            testCase.ConfigurePublishPipeline.Invoke(pipeline);
        }).Handle(evt, tokenSource.Token);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(Enumerable.Repeat(evt, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(expectedMiddlewareTypes));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EqualTo(expectedTransportTypes));
        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EqualTo(expectedTransportTypesFromPipelineBuilders));

        await dispatcher.DispatchEvent(evt, pipeline =>
        {
            if (testCase.ConfigurePublishPipeline is null)
            {
                return;
            }

            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

            testCase.ConfigurePublishPipeline.Invoke(pipeline);
        }, tokenSource.Token);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(Enumerable.Repeat(evt, testCase.ExpectedMiddlewareTypes.Count * 2)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count * 2)));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(expectedMiddlewareTypes.Concat(expectedMiddlewareTypes)));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EqualTo(expectedTransportTypes.Concat(expectedTransportTypes)));
        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EqualTo(expectedTransportTypesFromPipelineBuilders.Concat(expectedTransportTypesFromPipelineBuilders)));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase> GenerateTestCases()
    {
        // no middleware
        yield return new(null,
                         null,
                         null,
                         [],
                         []);

        // single middleware
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         p => p.Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                         ]);

        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);

        // delegate middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventMiddleware<TestEvent>));
                             observations.EventsFromMiddlewares.Add(ctx.Event);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Event, ctx.CancellationToken);
                         }),
                         null,
                         null,
                         [
                             (typeof(DelegateEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventMiddleware<TestEvent>));
                             observations.EventsFromMiddlewares.Add(ctx.Event);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Event, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventMiddleware<TestEvent>));
                             observations.EventsFromMiddlewares.Add(ctx.Event);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Event, ctx.CancellationToken);
                         }),
                         null,
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventMiddleware<TestEvent>));
                             observations.EventsFromMiddlewares.Add(ctx.Event);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Event, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(DelegateEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         p => p.Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                         ]);

        // mix delegate and normal middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventMiddleware<TestEvent>));
                             observations.EventsFromMiddlewares.Add(ctx.Event);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Event, ctx.CancellationToken);
                         }).Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         null,
                         [
                             (typeof(DelegateEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware2<TestEvent>>(),
                         null,
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware2<TestEvent>>(),

                         // verify that modifying a single observer pipeline does not affect other pipelines
                         p => p.Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware2<TestEvent>>(),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware2<TestEvent>>(),
                         null,
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware2<TestEvent>>(),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        // added on publish, added and removed in Observer
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware<TestEvent>>(),
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware<TestEvent>>()
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         null,
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventMiddleware<TestEvent>>()
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         null,
                         [
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                         ]);

        yield return new(p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),

                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);

        yield return new(null,
                         null,
                         p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                         ],
                         [
                             EventTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);

        yield return new(p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventRetryMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Publisher),
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventRetryMiddleware<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                             (typeof(TestEventMiddleware2<TestEvent>), EventTransportRole.Receiver),
                         ],
                         [
                             EventTransportRole.Publisher,
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                             EventTransportRole.Receiver,
                         ]);
    }

    [Test]
    public async Task GivenObserverPipelineWithMutatingMiddlewares_WhenObserverIsCalled_MiddlewaresCanChangeTheEventAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, ct) =>
                    {
                        await Task.CompletedTask;
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.EventsFromObservers.Add(evt);
                        obs.CancellationTokensFromObservers.Add(ct);
                    })
                    .AddSingleton(observations)
                    .AddSingleton(tokens)
                    .AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                        _ = pipeline.Use(new MutatingTestEventMiddleware<TestEvent>(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestEventMiddleware2<TestEvent>(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt1 = new TestEvent(0);
        var evt2 = new TestEvent(1);
        var evt3 = new TestEvent(3);

        await observer.Handle(evt1, tokens.CancellationTokens[0]);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt1, evt2 }));

        // the evt1 asserts that the middleware from the observer with the pipeline has no effect on the second observer
        Assert.That(observations.EventsFromObservers, Is.EqualTo(new[] { evt3, evt1 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromObservers, Is.EqualTo(new[] { tokens.CancellationTokens[2], tokens.CancellationTokens[0] }));

        await dispatcher.DispatchEvent(new TestEvent(0), tokens.CancellationTokens[0]);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt1, evt2, evt1, evt2 }));
        Assert.That(observations.EventsFromObservers, Is.EqualTo(new[] { evt3, evt1, evt3, evt1 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2).Concat(tokens.CancellationTokens.Take(2))));
        Assert.That(observations.CancellationTokensFromObservers, Is.EqualTo(new[] { tokens.CancellationTokens[2], tokens.CancellationTokens[0], tokens.CancellationTokens[2], tokens.CancellationTokens[0] }));
    }

    [Test]
    public async Task GivenPublishPipelineWithMutatingMiddlewares_WhenObserverIsCalled_MiddlewaresCanChangeTheEventAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, ct) =>
                    {
                        await Task.CompletedTask;
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.EventsFromObservers.Add(evt);
                        obs.CancellationTokensFromObservers.Add(ct);
                    })
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt1 = new TestEvent(0);
        var evt2 = new TestEvent(1);
        var evt3 = new TestEvent(3);

        await observer.WithPipeline(pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
            _ = pipeline.Use(new MutatingTestEventMiddleware<TestEvent>(obs, cancellationTokensToUse))
                        .Use(new MutatingTestEventMiddleware2<TestEvent>(obs, cancellationTokensToUse));
        }).Handle(evt1, tokens.CancellationTokens[0]);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt1, evt2 }));
        Assert.That(observations.EventsFromObservers, Is.EqualTo(new[] { evt3, evt3 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromObservers, Is.EqualTo(new[] { tokens.CancellationTokens[2], tokens.CancellationTokens[2] }));

        await dispatcher.DispatchEvent(new TestEvent(0), pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
            _ = pipeline.Use(new MutatingTestEventMiddleware<TestEvent>(obs, cancellationTokensToUse))
                        .Use(new MutatingTestEventMiddleware2<TestEvent>(obs, cancellationTokensToUse));
        }, tokens.CancellationTokens[0]);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt1, evt2, evt1, evt2 }));
        Assert.That(observations.EventsFromObservers, Is.EqualTo(new[] { evt3, evt3, evt3, evt3 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2).Concat(tokens.CancellationTokens.Take(2))));
        Assert.That(observations.CancellationTokensFromObservers, Is.EqualTo(new[] { tokens.CancellationTokens[2], tokens.CancellationTokens[2], tokens.CancellationTokens[2], tokens.CancellationTokens[2] }));
    }

    [Test]
    public void GivenObserverPipelineWithMiddlewareThatThrows_WhenObserverIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(exception)
                    .AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline => pipeline.Use(new ThrowingTestEventMiddleware<TestEvent>(exception)));

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        Assert.That(() => observer.Handle(new(10)), Throws.Exception.SameAs(exception));
        Assert.That(() => dispatcher.DispatchEvent(new TestEvent(10)), Throws.Exception.SameAs(exception));
    }

    [Test]
    public void GivenPublishPipelineWithMiddlewareThatThrows_WhenObserverIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        Assert.That(() => observer.WithPipeline(p => p.Use(new ThrowingTestEventMiddleware<TestEvent>(exception))).Handle(new(10)),
                    Throws.Exception.SameAs(exception));

        Assert.That(() => dispatcher.DispatchEvent(new TestEvent(10), p => p.Use(new ThrowingTestEventMiddleware<TestEvent>(exception))),
                    Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenObserverWithMiddlewares_WhenMiddlewareIsExecuted_ServiceProviderInContextAndPipelineConfigurationIsFromResolutionScope()
    {
        var services = new ServiceCollection();

        IServiceProvider? providerFromObserverPipelineBuild = null;
        IServiceProvider? providerFromObserverMiddleware = null;
        IServiceProvider? providerFromPublishPipelineBuild = null;
        IServiceProvider? providerFromClientMiddleware = null;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddTransient<TestObservations>()
                    .AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
                    {
                        providerFromObserverPipelineBuild = pipeline.ServiceProvider;
                        _ = pipeline.Use(ctx =>
                        {
                            providerFromObserverMiddleware = ctx.ServiceProvider;
                            return ctx.Next(ctx.Event, ctx.CancellationToken);
                        });
                    });

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IEventDispatcher>();

        await observer1.WithPipeline(pipeline =>
        {
            providerFromPublishPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Event, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromObserverPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromObserverMiddleware, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromPublishPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope1.ServiceProvider));

        await observer2.WithPipeline(pipeline =>
        {
            providerFromPublishPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Event, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromObserverPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromObserverPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromObserverMiddleware, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromPublishPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope2.ServiceProvider));

        await dispatcher1.DispatchEvent(new TestEvent(10), pipeline =>
        {
            providerFromPublishPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Event, ctx.CancellationToken);
            });
        });

        Assert.That(providerFromObserverPipelineBuild, Is.Not.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromObserverPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromObserverMiddleware, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromPublishPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope1.ServiceProvider));

        await dispatcher2.DispatchEvent(new TestEvent(10), pipeline =>
        {
            providerFromPublishPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Event, ctx.CancellationToken);
            });
        });

        Assert.That(providerFromObserverPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromObserverPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromObserverMiddleware, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromPublishPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope2.ServiceProvider));
    }

    [Test]
    public async Task GivenMultiplePublishPipelineConfigurations_WhenObserverIsCalled_PipelinesAreExecutedInReverseOrder()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.WithPipeline(p => p.Use(new TestEventMiddleware<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                      .WithPipeline(p => p.Use(new TestEventMiddleware2<TestEvent>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                      .Handle(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventMiddleware2<TestEvent>), typeof(TestEventMiddleware<TestEvent>) }));
    }

    [Test]
    public async Task GivenObserverAndPublishPipeline_WhenObserverIsCalled_TransportTypesInPipelinesAreCorrect()
    {
        var services = new ServiceCollection();
        EventTransportType? transportTypeFromPublish = null;
        EventTransportType? transportTypeFromObserver = null;

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (_, _, _) => { await Task.Yield(); },
                                                                  pipeline => transportTypeFromObserver = pipeline.TransportType);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent(10);

        await observer.WithPipeline(pipeline => transportTypeFromPublish = pipeline.TransportType).Handle(evt);

        Assert.That(transportTypeFromPublish, Is.EqualTo(new EventTransportType(InProcessEventAttribute.TransportName, EventTransportRole.Publisher)));
        Assert.That(transportTypeFromObserver, Is.EqualTo(new EventTransportType(InProcessEventAttribute.TransportName, EventTransportRole.Receiver)));

        transportTypeFromPublish = null;
        transportTypeFromObserver = null;

        await dispatcher.DispatchEvent(evt, pipeline => transportTypeFromPublish = pipeline.TransportType);

        Assert.That(transportTypeFromPublish, Is.EqualTo(new EventTransportType(InProcessEventAttribute.TransportName, EventTransportRole.Publisher)));
        Assert.That(transportTypeFromObserver, Is.EqualTo(new EventTransportType(InProcessEventAttribute.TransportName, EventTransportRole.Receiver)));
    }

    [Test]
    public async Task GivenObserverAndPublishPipeline_WhenPipelineIsBeingBuilt_MiddlewaresCanBeEnumerated()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask,
                                                                  pipeline =>
                                                                  {
                                                                      var middleware1 = new TestEventMiddleware<TestEvent>(new());
                                                                      var middleware2 = new TestEventMiddleware2<TestEvent>(new());
                                                                      _ = pipeline.Use(middleware1).Use(middleware2);

                                                                      Assert.That(pipeline, Has.Count.EqualTo(2));
                                                                      Assert.That(pipeline, Is.EqualTo(new IEventMiddleware<TestEvent>[] { middleware1, middleware2 }));
                                                                  });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEvent(10);

        await observer.WithPipeline(pipeline =>
                      {
                          var middleware1 = new TestEventMiddleware<TestEvent>(new());
                          var middleware2 = new TestEventMiddleware2<TestEvent>(new());
                          _ = pipeline.Use(middleware1).Use(middleware2);

                          Assert.That(pipeline, Has.Count.EqualTo(2));
                          Assert.That(pipeline, Is.EqualTo(new IEventMiddleware<TestEvent>[] { middleware1, middleware2 }));
                      })
                      .Handle(evt);

        await dispatcher.DispatchEvent(evt, pipeline =>
        {
            var middleware1 = new TestEventMiddleware<TestEvent>(new());
            var middleware2 = new TestEventMiddleware2<TestEvent>(new());
            _ = pipeline.Use(middleware1).Use(middleware2);

            Assert.That(pipeline, Has.Count.EqualTo(2));
            Assert.That(pipeline, Is.EqualTo(new IEventMiddleware<TestEvent>[] { middleware1, middleware2 }));
        });
    }

    [Test]
    public async Task GivenPublishPipelineForEventTypeWithMultipleCustomTransports_WhenPublishingEvent_PublishMiddlewaresAreCalledOncePerTransport()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEventWithCustomPublishers();

        await observer.WithPipeline(pipeline =>
                      {
                          var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                          obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

                          _ = pipeline.Use(new TestEventMiddleware<TestEventWithCustomPublishers>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                      })
                      .Handle(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventMiddleware<TestEventWithCustomPublishers>),
            typeof(TestEventMiddleware<TestEventWithCustomPublishers>),
        }));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EquivalentTo(new[]
        {
            new EventTransportType(nameof(TestEventTransportAttribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransport2Attribute), EventTransportRole.Publisher),
        }));

        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EquivalentTo(new[]
        {
            new EventTransportType(nameof(TestEventTransportAttribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransport2Attribute), EventTransportRole.Publisher),
        }));

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[]
        {
            (typeof(TestEventTransportPublisher), evt),
            (typeof(TestEventTransportPublisher2), evt),
        }));

        await dispatcher.DispatchEvent(evt, pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

            _ = pipeline.Use(new TestEventMiddleware<TestEventWithCustomPublishers>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
        });

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventMiddleware<TestEventWithCustomPublishers>),
            typeof(TestEventMiddleware<TestEventWithCustomPublishers>),
            typeof(TestEventMiddleware<TestEventWithCustomPublishers>),
            typeof(TestEventMiddleware<TestEventWithCustomPublishers>),
        }));

        Assert.That(observations.TransportTypesFromMiddlewares, Is.EquivalentTo(new[]
        {
            new EventTransportType(nameof(TestEventTransportAttribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransport2Attribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransportAttribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransport2Attribute), EventTransportRole.Publisher),
        }));

        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EquivalentTo(new[]
        {
            new EventTransportType(nameof(TestEventTransportAttribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransport2Attribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransportAttribute), EventTransportRole.Publisher),
            new EventTransportType(nameof(TestEventTransport2Attribute), EventTransportRole.Publisher),
        }));

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[]
        {
            (typeof(TestEventTransportPublisher), evt),
            (typeof(TestEventTransportPublisher2), evt),
            (typeof(TestEventTransportPublisher), evt),
            (typeof(TestEventTransportPublisher2), evt),
        }));
    }

    [Test]
    [Combinatorial]
    public void GivenPublishPipelineForEventTypeWithMultipleCustomTransports_WhenPublishingEventThrowsException_NonThrowingTransportsAreStillExecutedAndInvocationThrowsCorrectException(
        [Values(true, false)] bool throwInAll,
        [Values("publisher", "pipeline")] string throwLocation)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exceptionForMiddleware = new Exception();
        var exception1 = new Exception1();
        var exception2 = new Exception2();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        if (throwLocation == "publisher")
        {
            _ = services.AddSingleton(exception1);

            if (throwInAll)
            {
                _ = services.AddSingleton(exception2);
            }
        }

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEventWithCustomPublishers();

        var thrownException = Assert.CatchAsync<Exception>(
            () => observer.WithPipeline(pipeline =>
                          {
                              if (throwLocation != "pipeline" || (!throwInAll && pipeline.TransportType.Name == nameof(TestEventTransport2Attribute)))
                              {
                                  return;
                              }

                              _ = pipeline.Use(new ThrowingTestEventMiddleware<TestEventWithCustomPublishers>(exceptionForMiddleware));
                          })
                          .Handle(evt));

        switch (throwLocation, throwInAll)
        {
            case ("publisher", true):
                Assert.That(thrownException, Is.InstanceOf<AggregateException>());
                Assert.That(((AggregateException)thrownException).InnerExceptions, Is.EquivalentTo(new Exception[] { exception1, exception2 }));
                Assert.That(observations.ObservedPublishes, Is.Empty);
                break;
            case ("publisher", false):
                Assert.That(thrownException, Is.SameAs(exception1));
                Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[] { (typeof(TestEventTransportPublisher2), evt) }));
                break;
            case ("pipeline", true):
                Assert.That(thrownException, Is.InstanceOf<AggregateException>());
                Assert.That(((AggregateException)thrownException).InnerExceptions, Is.EquivalentTo(new[] { exceptionForMiddleware, exceptionForMiddleware }));
                Assert.That(observations.ObservedPublishes, Is.Empty);
                break;
            case ("pipeline", false):
                Assert.That(thrownException, Is.SameAs(exceptionForMiddleware));
                Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[] { (typeof(TestEventTransportPublisher2), evt) }));
                break;
        }

        observations.ObservedPublishes.Clear();

        thrownException = Assert.CatchAsync<Exception>(
            () => dispatcher.DispatchEvent(evt, pipeline =>
            {
                if (throwLocation != "pipeline" || (!throwInAll && pipeline.TransportType.Name == nameof(TestEventTransport2Attribute)))
                {
                    return;
                }

                _ = pipeline.Use(new ThrowingTestEventMiddleware<TestEventWithCustomPublishers>(exceptionForMiddleware));
            }));

        switch (throwLocation, throwInAll)
        {
            case ("publisher", true):
                Assert.That(thrownException, Is.InstanceOf<AggregateException>());
                Assert.That(((AggregateException)thrownException).InnerExceptions, Is.EquivalentTo(new Exception[] { exception1, exception2 }));
                Assert.That(observations.ObservedPublishes, Is.Empty);
                break;
            case ("publisher", false):
                Assert.That(thrownException, Is.SameAs(exception1));
                Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[] { (typeof(TestEventTransportPublisher2), evt) }));
                break;
            case ("pipeline", true):
                Assert.That(thrownException, Is.InstanceOf<AggregateException>());
                Assert.That(((AggregateException)thrownException).InnerExceptions, Is.EquivalentTo(new[] { exceptionForMiddleware, exceptionForMiddleware }));
                Assert.That(observations.ObservedPublishes, Is.Empty);
                break;
            case ("pipeline", false):
                Assert.That(thrownException, Is.SameAs(exceptionForMiddleware));
                Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[] { (typeof(TestEventTransportPublisher2), evt) }));
                break;
        }
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransport_WhenObserversAreCalledFromBroadcaster_MiddlewaresAreCalledWithEventAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => Task.CompletedTask, pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        _ = pipeline.Use(new TestEventMiddleware<TestEvent>(obs))
                                    .Use(new TestEventMiddleware2<TestEvent>(obs));
                    })
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        _ = pipeline.Use(new TestEventMiddleware<TestEvent>(obs))
                                    .Use(new TestEventMiddleware2<TestEvent>(obs));
                    });

        var provider = services.BuildServiceProvider();

        var broadcaster = provider.GetRequiredService<IEventTransportReceiverBroadcaster>();

        var evt = new TestEvent(0);

        using var cts = new CancellationTokenSource();

        await broadcaster.Broadcast(evt, new TestEventTransportAttribute(), provider, cts.Token);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(new[] { cts.Token, cts.Token, cts.Token, cts.Token }));

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[]
        {
            typeof(TestEventMiddleware<TestEvent>),
            typeof(TestEventMiddleware2<TestEvent>),
            typeof(TestEventMiddleware<TestEvent>),
            typeof(TestEventMiddleware2<TestEvent>),
        }));
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransport_WhenPublishing_PublishersAreResolvedPerMiddlewareExecution()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new TestEventWithCustomPublishers();

        await observer.WithPipeline(pipeline =>
                      {
                          var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                          _ = pipeline.Use(new TestEventRetryMiddleware<TestEventWithCustomPublishers>(obs));
                      })
                      .Handle(evt);

        Assert.That(observations.PublisherInstances.Distinct().ToList(), Has.Count.EqualTo(4));

        await dispatcher.DispatchEvent(evt, pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            _ = pipeline.Use(new TestEventRetryMiddleware<TestEventWithCustomPublishers>(obs));
        });

        Assert.That(observations.PublisherInstances.Distinct().ToList(), Has.Count.EqualTo(8));
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransport_WhenObserversAreCalledFromBroadcaster_ObserversAreResolvedPerMiddlewareExecution()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        _ = pipeline.Use(new TestEventRetryMiddleware<TestEvent>(obs));
                    });

        var provider = services.BuildServiceProvider();

        var broadcaster = provider.GetRequiredService<IEventTransportReceiverBroadcaster>();

        var evt = new TestEvent(0);

        await broadcaster.Broadcast(evt, new TestEventTransportAttribute(), provider, CancellationToken.None);

        Assert.That(observations.ObserverInstances.Distinct().ToList(), Has.Count.EqualTo(4));
    }

    [Test]
    public async Task GivenPublishAndObserverPipelinesForBaseType_WhenObserverIsCalledWithSubType_ObserverMiddlewaresAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventBaseObserver>()
                    .AddConquerorEventObserver<TestEventSubObserver>()
                    .AddConquerorEventObserverDelegate<TestEventBase>((_, _, _) => Task.CompletedTask, pipeline =>
                    {
                        _ = pipeline.Use(new TestEventMiddleware<TestEventBase>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                    })
                    .AddConquerorEventObserverDelegate<TestEventSub>((_, _, _) => Task.CompletedTask, pipeline =>
                    {
                        _ = pipeline.Use(new TestEventMiddleware<TestEventSub>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                    })
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventPipeline<TestEventBase>>>(pipeline =>
                    {
                        _ = pipeline.Use(new TestEventMiddleware<TestEventBase>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                    })
                    .AddSingleton<Action<IEventPipeline<TestEventSub>>>(pipeline =>
                    {
                        _ = pipeline.Use(new TestEventMiddleware<TestEventSub>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventBase>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        await observer.WithPipeline(p => p.Use(new TestEventMiddleware<TestEventBase>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                      .Handle(new TestEventSubSub(10));

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventSub>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventSub>),
        }));

        await dispatcher.DispatchEvent<TestEventBase>(new TestEventSubSub(10), p => p.Use(new TestEventMiddleware<TestEventBase>(p.ServiceProvider.GetRequiredService<TestObservations>())));

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventSub>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventSub>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventSub>),
            typeof(TestEventMiddleware<TestEventBase>),
            typeof(TestEventMiddleware<TestEventSub>),
        }));
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase(
        Action<IEventPipeline<TestEvent>>? ConfigureObserver1Pipeline,
        Action<IEventPipeline<TestEvent>>? ConfigureObserver2Pipeline,
        Action<IEventPipeline<TestEvent>>? ConfigurePublishPipeline,
        IReadOnlyCollection<(Type MiddlewareType, EventTransportRole TransportRole)> ExpectedMiddlewareTypes,
        IReadOnlyCollection<EventTransportRole> ExpectedTransportRolesFromPipelineBuilders);

    public sealed record TestEvent(int Payload);

    public abstract record TestEventBase(int Payload);

    public abstract record TestEventSub(int Payload) : TestEventBase(Payload);

    public sealed record TestEventSubSub(int Payload) : TestEventSub(Payload);

    [TestEventTransport]
    private sealed record TestEventWithCustomPublisher;

    [TestEventTransport]
    [TestEventTransport2]
    private sealed record TestEventWithCustomPublishers;

    private sealed class TestEventObserver(TestObservations observations) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
            observations.ObserverInstances.Add(this);
        }

        public static void ConfigurePipeline(IEventPipeline<TestEvent> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<TestEvent>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestEventObserver2(TestObservations observations) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
            observations.ObserverInstances.Add(this);
        }

        public static void ConfigurePipeline(IEventPipeline<TestEvent> pipeline)
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

            pipeline.ServiceProvider.GetService<Action<IEventPipeline<TestEvent>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestEventBaseObserver : IEventObserver<TestEventBase>
    {
        public async Task Handle(TestEventBase evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventPipeline<TestEventBase> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<TestEventBase>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestEventSubObserver : IEventObserver<TestEventSub>
    {
        public async Task Handle(TestEventSub evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventPipeline<TestEventSub> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<TestEventSub>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestEventMiddleware<TEvent>(TestObservations observations) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventMiddleware2<TEvent>(TestObservations observations) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventRetryMiddleware<TEvent>(TestObservations observations) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestEventMiddleware<TEvent>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var evt = ctx.Event;

            if (evt is TestEvent testEvent)
            {
                evt = (TEvent)(object)new TestEvent(testEvent.Payload + 1);
            }

            await ctx.Next(evt, cancellationTokensToUse.CancellationTokens[1]);
        }
    }

    private sealed class MutatingTestEventMiddleware2<TEvent>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var evt = ctx.Event;

            if (evt is TestEvent testEvent)
            {
                evt = (TEvent)(object)new TestEvent(testEvent.Payload + 2);
            }

            await ctx.Next(evt, cancellationTokensToUse.CancellationTokens[2]);
        }
    }

    private sealed class ThrowingTestEventMiddleware<TEvent>(Exception exception) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    // only used as a marker for pipeline type check
    private sealed class DelegateEventMiddleware<TEvent> : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public Task Execute(EventMiddlewareContext<TEvent> ctx) => throw new NotSupportedException();
    }

    private sealed class Exception1 : Exception;

    private sealed class Exception2 : Exception;

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute() : EventTransportAttribute(nameof(TestEventTransportAttribute));

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransport2Attribute() : EventTransportAttribute(nameof(TestEventTransport2Attribute));

    private sealed class TestEventTransportPublisher(TestObservations observations, Exception1? exceptionToThrow = null)
        : IEventTransportPublisher<TestEventTransportAttribute>
    {
        public async Task PublishEvent(object evt, TestEventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.ObservedPublishes.Enqueue((GetType(), evt));
            observations.PublisherInstances.Enqueue(this);
        }
    }

    private sealed class TestEventTransportPublisher2(TestObservations observations, Exception2? exceptionToThrow = null) : IEventTransportPublisher<TestEventTransport2Attribute>
    {
        public async Task PublishEvent(object evt, TestEventTransport2Attribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.ObservedPublishes.Enqueue((GetType(), evt));
            observations.PublisherInstances.Enqueue(this);
        }
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> EventsFromObservers { get; } = [];

        public List<object> EventsFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromObservers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<EventTransportType> TransportTypesFromPipelineBuilders { get; } = [];

        public List<EventTransportType> TransportTypesFromMiddlewares { get; } = [];

        public List<object> ObserverInstances { get; } = [];

        public ConcurrentQueue<(Type PublisherType, object Event)> ObservedPublishes { get; } = new();

        public ConcurrentQueue<object> PublisherInstances { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
