namespace Conqueror.Tests.Eventing;

public sealed partial class EventNotificationMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenPublisherAndHandlerPipelines_WhenHandlerIsCalled_MiddlewaresAreCalledWithEventNotification(
        ConquerorMiddlewareFunctionalityTestCase<TestEventNotification> testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline =>
                    {
                        if (testCase.ConfigureHandlerPipeline is null)
                        {
                            return;
                        }

                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

                        testCase.ConfigureHandlerPipeline?.Invoke(pipeline);
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var expectedTransportTypesFromPipelineBuilders = testCase.ExpectedTransportRolesFromPipelineBuilders
                                                                 .Select(r => new EventNotificationTransportType(ConquerorConstants.InProcessTransportName, r))
                                                                 .ToList();

        using var tokenSource = new CancellationTokenSource();

        var notification = new TestEventNotification(10);

        await handler.WithPipeline(pipeline =>
        {
            if (testCase.ConfigurePublisherPipeline is null)
            {
                return;
            }

            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

            testCase.ConfigurePublisherPipeline?.Invoke(pipeline);
        }).Handle(notification, tokenSource.Token);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(Enumerable.Repeat(notification, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares,
                    Is.EqualTo(testCase.ExpectedMiddlewareTypes
                                       .Select(t => new EventNotificationTransportType(ConquerorConstants.InProcessTransportName, t.TransportRole))));
        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EqualTo(expectedTransportTypesFromPipelineBuilders));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TestEventNotification>> GenerateTestCases()
        => GenerateTestCasesGeneric<TestEventNotification>();

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TEventNotification>> GenerateTestCasesGeneric<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        // no middleware
        yield return new(null,
                         null,
                         [],
                         []);

        // single middleware
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                             EventNotificationTransportRole.Receiver,
                         ]);

        // delegate middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventNotificationMiddleware<TEventNotification>));
                             observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
                         }),
                         null,
                         [
                             (typeof(DelegateEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventNotificationMiddleware<TEventNotification>));
                             observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventNotificationMiddleware<TEventNotification>));
                             observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
                         }),
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventNotificationMiddleware<TEventNotification>));
                             observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(DelegateEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                             EventNotificationTransportRole.Receiver,
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                             EventNotificationTransportRole.Receiver,
                         ]);

        // mix delegate and normal middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateEventNotificationMiddleware<TEventNotification>));
                             observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
                         }).Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(DelegateEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware2<TEventNotification>>(),
                         null,
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware2<TEventNotification>>(),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware2<TEventNotification>>(),
                         null,
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware2<TEventNotification>>(),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware<TEventNotification>>(),
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                             EventNotificationTransportRole.Receiver,
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware<TEventNotification>>()
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestEventNotificationMiddleware<TEventNotification>>()
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestEventNotificationRetryMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestEventNotificationRetryMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestEventNotificationRetryMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationRetryMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestEventNotificationRetryMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestEventNotificationRetryMiddleware<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestEventNotificationMiddleware2<TEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestEventNotificationRetryMiddleware<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationRetryMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware2<TEventNotification>), EventNotificationTransportRole.Publisher),
                             (typeof(TestEventNotificationRetryMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                             (typeof(TestEventNotificationMiddleware<TEventNotification>), EventNotificationTransportRole.Receiver),
                         ],
                         [
                             EventNotificationTransportRole.Publisher,
                             EventNotificationTransportRole.Receiver,
                             EventNotificationTransportRole.Receiver,
                         ]);
    }

    [Test]
    public async Task GivenHandlerPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheEventNotificationAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens)
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                        _ = pipeline.Use(new MutatingTestEventNotificationMiddleware<TestEventNotification>(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestEventNotificationMiddleware2<TestEventNotification>(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.Handle(new(0), tokens.CancellationTokens[0]);

        var notification1 = new TestEventNotification(0);
        var notification2 = new TestEventNotification(1);
        var notification3 = new TestEventNotification(3);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(new[] { notification1, notification2 }));
        Assert.That(observations.EventNotificationsFromHandlers, Is.EqualTo(new[] { notification3 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPublisherPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheEventNotificationAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.WithPipeline(pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
            _ = pipeline.Use(new MutatingTestEventNotificationMiddleware<TestEventNotification>(obs, cancellationTokensToUse))
                        .Use(new MutatingTestEventNotificationMiddleware2<TestEventNotification>(obs, cancellationTokensToUse));
        }).Handle(new(0), tokens.CancellationTokens[0]);

        var notification1 = new TestEventNotification(0);
        var notification2 = new TestEventNotification(1);
        var notification3 = new TestEventNotification(3);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(new[] { notification1, notification2 }));
        Assert.That(observations.EventNotificationsFromHandlers, Is.EqualTo(new[] { notification3 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public void GivenHandlerPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(exception)
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline => pipeline.Use(new ThrowingTestEventNotificationMiddleware<TestEventNotification>(exception)));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenPublisherPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestEventNotificationMiddleware<TestEventNotification>(exception))).Handle(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandlerWithMiddlewares_WhenMiddlewareIsExecuted_ServiceProviderInContextAndPipelineConfigurationIsFromResolutionScope()
    {
        var services = new ServiceCollection();

        IServiceProvider? providerFromHandlerPipelineBuild = null;
        IServiceProvider? providerFromHandlerMiddleware = null;
        IServiceProvider? providerFromPublisherPipelineBuild = null;
        IServiceProvider? providerFromPublisherMiddleware = null;

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddTransient<TestObservations>()
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline =>
                    {
                        providerFromHandlerPipelineBuild = pipeline.ServiceProvider;
                        _ = pipeline.Use(ctx =>
                        {
                            providerFromHandlerMiddleware = ctx.ServiceProvider;
                            return ctx.Next(ctx.EventNotification, ctx.CancellationToken);
                        });
                    });

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider
                             .GetRequiredService<IEventNotificationPublishers>()
                             .For(TestEventNotification.T);

        var handler2 = scope2.ServiceProvider
                             .GetRequiredService<IEventNotificationPublishers>()
                             .For(TestEventNotification.T);

        await handler1.WithPipeline(pipeline =>
        {
            providerFromPublisherPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromPublisherMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.EventNotification, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromHandlerPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromPublisherPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromPublisherMiddleware, Is.SameAs(scope1.ServiceProvider));

        await handler2.WithPipeline(pipeline =>
        {
            providerFromPublisherPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromPublisherMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.EventNotification, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromHandlerPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromPublisherPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromPublisherMiddleware, Is.SameAs(scope2.ServiceProvider));
    }

    [Test]
    public async Task GivenMultiplePublisherPipelineConfigurations_WhenHandlerIsCalled_PipelinesAreExecutedInOrder()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.WithPipeline(p => p.Use(new TestEventNotificationMiddleware<TestEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                     .WithPipeline(p => p.Use(new TestEventNotificationMiddleware2<TestEventNotification>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                     .Handle(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventNotificationMiddleware<TestEventNotification>),
            typeof(TestEventNotificationMiddleware2<TestEventNotification>),
        }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithEventNotification()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandlerDelegate(
                        TestEventNotification.T,
                        async (notification, p, cancellationToken) =>
                        {
                            await Task.Yield();
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.EventNotificationsFromHandlers.Add(notification);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        }, pipeline => pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var notification = new TestEventNotification(10);

        await handler.Handle(notification);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(new[] { notification }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventNotificationMiddleware<TestEventNotification>) }));
    }

    [Test]
    public async Task GivenSyncHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithEventNotification()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandlerDelegate(
                        TestEventNotification.T,
                        (notification, p, cancellationToken) =>
                        {
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.EventNotificationsFromHandlers.Add(notification);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        }, pipeline => pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var notification = new TestEventNotification(10);

        await handler.Handle(notification);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(new[] { notification }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventNotificationMiddleware<TestEventNotification>) }));
    }

    [Test]
    public async Task GivenHandlerForEventNotificationBaseTypeWithSingleAppliedMiddleware_WhenHandlerIsCalledWithEventNotificationSubType_MiddlewareIsCalledWithEventNotification()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationBaseHandler>()
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotificationBase>>>(pipeline => pipeline.Use(new TestEventNotificationMiddleware<TestEventNotificationBase>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotificationBase.T);

        var notification = new TestEventNotificationSub(10, -1);

        await handler.Handle(notification);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(new[] { notification }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventNotificationMiddleware<TestEventNotificationBase>) }));
    }

    [Test]
    public async Task GivenHandlerRegisteredViaAssemblyScanningWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithEventNotification()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandlersFromExecutingAssembly()
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline => pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var notification = new TestEventNotification(10);

        await handler.Handle(notification);

        Assert.That(observations.EventNotificationsFromMiddlewares, Is.EqualTo(new[] { notification }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventNotificationMiddleware<TestEventNotification>) }));
    }

    [Test]
    public async Task GivenHandlerAndPublisherPipeline_WhenHandlerIsCalled_TransportTypesInPipelinesAreCorrect()
    {
        var services = new ServiceCollection();
        EventNotificationTransportType? transportTypeFromPublisher = null;
        EventNotificationTransportType? transportTypeFromHandler = null;

        _ = services.AddEventNotificationHandlerDelegate(
            TestEventNotification.T,
            async (_, _, _) => { await Task.Yield(); },
            pipeline => transportTypeFromHandler = pipeline.TransportType);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var notification = new TestEventNotification(10);

        await handler.WithPipeline(pipeline => transportTypeFromPublisher = pipeline.TransportType).Handle(notification);

        Assert.That(transportTypeFromPublisher, Is.EqualTo(new EventNotificationTransportType(ConquerorConstants.InProcessTransportName, EventNotificationTransportRole.Publisher)));
        Assert.That(transportTypeFromHandler, Is.EqualTo(new EventNotificationTransportType(ConquerorConstants.InProcessTransportName, EventNotificationTransportRole.Receiver)));
    }

    [Test]
    public async Task GivenHandlerAndPublisherPipeline_WhenPipelineIsBeingBuilt_MiddlewaresCanBeEnumerated()
    {
        var services = new ServiceCollection();

        _ = services.AddEventNotificationHandlerDelegate(
            TestEventNotification.T,
            (_, _, _) => Task.CompletedTask,
            pipeline =>
            {
                var middleware1 = new TestEventNotificationMiddleware<TestEventNotification>(new());
                var middleware2 = new TestEventNotificationMiddleware2<TestEventNotification>(new());
                _ = pipeline.Use(middleware1).Use(middleware2);

                Assert.That(pipeline, Has.Count.EqualTo(2));
                Assert.That(pipeline, Is.EqualTo(new IEventNotificationMiddleware<TestEventNotification>[] { middleware1, middleware2 }));
            });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        var notification = new TestEventNotification(10);

        await handler.WithPipeline(pipeline =>
                     {
                         var middleware1 = new TestEventNotificationMiddleware<TestEventNotification>(new());
                         var middleware2 = new TestEventNotificationMiddleware2<TestEventNotification>(new());
                         _ = pipeline.Use(middleware1).Use(middleware2);

                         Assert.That(pipeline, Has.Count.EqualTo(2));
                         Assert.That(pipeline, Is.EqualTo(new IEventNotificationMiddleware<TestEventNotification>[] { middleware1, middleware2 }));
                     })
                     .Handle(notification);
    }

    [Test]
    public async Task GivenHandlerWithMultipleEventTypes_WhenHandlerIsCalled_PipelineBuilderIsCalledForTheCorrectType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var builder1WasCalled = false;
        var builder2WasCalled = false;

        _ = services.AddEventNotificationHandler<MultiTestEventNotificationHandler>()
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(_ => builder1WasCalled = true)
                    .AddSingleton<Action<IEventNotificationPipeline<TestEventNotification2>>>(_ => builder2WasCalled = true)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IEventNotificationPublishers>()
                      .For(TestEventNotification.T).Handle(new(10));

        Assert.That(builder1WasCalled, Is.True);
        Assert.That(builder2WasCalled, Is.False);

        await provider.GetRequiredService<IEventNotificationPublishers>()
                      .For(TestEventNotification2.T).Handle(new());

        Assert.That(builder2WasCalled, Is.True);
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase<TEventNotification>(
        Action<IEventNotificationPipeline<TEventNotification>>? ConfigureHandlerPipeline,
        Action<IEventNotificationPipeline<TEventNotification>>? ConfigurePublisherPipeline,
        IReadOnlyCollection<(Type MiddlewareType, EventNotificationTransportRole TransportRole)> ExpectedMiddlewareTypes,
        IReadOnlyCollection<EventNotificationTransportRole> ExpectedTransportRolesFromPipelineBuilders)
        where TEventNotification : class, IEventNotification<TEventNotification>;

    [EventNotification]
    public sealed partial record TestEventNotification(int Payload);

    [EventNotification]
    private sealed partial record TestEventNotification2;

    private sealed class TestEventNotificationHandler(TestObservations observations) : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventNotificationsFromHandlers.Add(notification);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
        {
            if (pipeline is IEventNotificationPipeline<TestEventNotification> p)
            {
                pipeline.ServiceProvider.GetService<Action<IEventNotificationPipeline<TestEventNotification>>>()?.Invoke(p);
            }
        }
    }

    [EventNotification]
    private partial record TestEventNotificationBase(int PayloadBase);

    private sealed record TestEventNotificationSub(int PayloadBase, int PayloadSub) : TestEventNotificationBase(PayloadBase);

    private sealed class TestEventNotificationBaseHandler(TestObservations observations) : TestEventNotificationBase.IHandler
    {
        public async Task Handle(TestEventNotificationBase notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventNotificationsFromHandlers.Add(notification);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
        {
            if (pipeline is IEventNotificationPipeline<TestEventNotificationBase> p)
            {
                pipeline.ServiceProvider.GetService<Action<IEventNotificationPipeline<TestEventNotificationBase>>>()?.Invoke(p);
            }
        }
    }

    private sealed class MultiTestEventNotificationHandler : TestEventNotification.IHandler, TestEventNotification2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public async Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
        {
            if (pipeline is IEventNotificationPipeline<TestEventNotification> p)
            {
                pipeline.ServiceProvider.GetService<Action<IEventNotificationPipeline<TestEventNotification>>>()?.Invoke(p);
            }

            if (pipeline is IEventNotificationPipeline<TestEventNotification2> p2)
            {
                pipeline.ServiceProvider.GetService<Action<IEventNotificationPipeline<TestEventNotification2>>>()?.Invoke(p2);
            }
        }
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed class TestEventNotificationForAssemblyScanningHandler(TestObservations observations)
        : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventNotificationsFromHandlers.Add(notification);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
        {
            if (pipeline is IEventNotificationPipeline<TestEventNotification> p)
            {
                pipeline.ServiceProvider.GetService<Action<IEventNotificationPipeline<TestEventNotification>>>()?.Invoke(p);
            }
        }
    }

    private sealed class TestEventNotificationMiddleware<TEventNotification>(TestObservations observations) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
        }
    }

    private sealed class TestEventNotificationMiddleware2<TEventNotification>(TestObservations observations) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
        }
    }

    private sealed class TestEventNotificationRetryMiddleware<TEventNotification>(TestObservations observations) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
            await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestEventNotificationMiddleware<TEventNotification>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var notification = ctx.EventNotification;

            if (notification is TestEventNotification testEventNotification)
            {
                notification = (TEventNotification)(object)new TestEventNotification(testEventNotification.Payload + 1);
            }

            await ctx.Next(notification, cancellationTokensToUse.CancellationTokens[1]);
        }
    }

    private sealed class MutatingTestEventNotificationMiddleware2<TEventNotification>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventNotificationsFromMiddlewares.Add(ctx.EventNotification);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var notification = ctx.EventNotification;

            if (notification is TestEventNotification testEventNotification)
            {
                notification = (TEventNotification)(object)new TestEventNotification(testEventNotification.Payload + 2);
            }

            await ctx.Next(notification, cancellationTokensToUse.CancellationTokens[2]);
        }
    }

    private sealed class ThrowingTestEventNotificationMiddleware<TEventNotification>(Exception exception) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    // only used as a marker for pipeline type check
    private sealed class DelegateEventNotificationMiddleware<TEventNotification> : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx) => throw new NotSupportedException();
    }

    public sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> EventNotificationsFromHandlers { get; } = [];

        public List<object> EventNotificationsFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<EventNotificationTransportType> TransportTypesFromPipelineBuilders { get; } = [];

        public List<EventNotificationTransportType> TransportTypesFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
