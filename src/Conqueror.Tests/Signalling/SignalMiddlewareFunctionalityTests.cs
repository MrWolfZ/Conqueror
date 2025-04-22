namespace Conqueror.Tests.Signalling;

public sealed partial class SignalMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenPublisherAndHandlerPipelines_WhenHandlerIsCalled_MiddlewaresAreCalledWithSignal(
        ConquerorMiddlewareFunctionalityTestCase<TestSignal> testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline =>
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

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var expectedTransportTypesFromPipelineBuilders = testCase.ExpectedTransportRolesFromPipelineBuilders
                                                                 .Select(r => new SignalTransportType(ConquerorConstants.InProcessTransportName, r))
                                                                 .ToList();

        using var tokenSource = new CancellationTokenSource();

        var signal = new TestSignal(10);

        await handler.WithPipeline(pipeline =>
        {
            if (testCase.ConfigurePublisherPipeline is null)
            {
                return;
            }

            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);

            testCase.ConfigurePublisherPipeline?.Invoke(pipeline);
        }).Handle(signal, tokenSource.Token);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(Enumerable.Repeat(signal, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares,
                    Is.EqualTo(testCase.ExpectedMiddlewareTypes
                                       .Select(t => new SignalTransportType(ConquerorConstants.InProcessTransportName, t.TransportRole))));
        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EqualTo(expectedTransportTypesFromPipelineBuilders));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TestSignal>> GenerateTestCases()
        => GenerateTestCasesGeneric<TestSignal>();

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TSignal>> GenerateTestCasesGeneric<TSignal>()
        where TSignal : class, ISignal<TSignal>
    {
        // no middleware
        yield return new(null,
                         null,
                         [],
                         []);

        // single middleware
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Publisher,
                             SignalTransportRole.Receiver,
                         ]);

        // delegate middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateSignalMiddleware<TSignal>));
                             observations.SignalsFromMiddlewares.Add(ctx.Signal);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Signal, ctx.CancellationToken);
                         }),
                         null,
                         [
                             (typeof(DelegateSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateSignalMiddleware<TSignal>));
                             observations.SignalsFromMiddlewares.Add(ctx.Signal);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Signal, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateSignalMiddleware<TSignal>));
                             observations.SignalsFromMiddlewares.Add(ctx.Signal);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Signal, ctx.CancellationToken);
                         }),
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateSignalMiddleware<TSignal>));
                             observations.SignalsFromMiddlewares.Add(ctx.Signal);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Signal, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(DelegateSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Publisher,
                             SignalTransportRole.Receiver,
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Publisher,
                             SignalTransportRole.Receiver,
                         ]);

        // mix delegate and normal middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateSignalMiddleware<TSignal>));
                             observations.SignalsFromMiddlewares.Add(ctx.Signal);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             await ctx.Next(ctx.Signal, ctx.CancellationToken);
                         }).Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(DelegateSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware2<TSignal>>(),
                         null,
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware2<TSignal>>(),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware2<TSignal>>(),
                         null,
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware2<TSignal>>(),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware<TSignal>>(),
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                             SignalTransportRole.Receiver,
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware<TSignal>>()
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestSignalMiddleware<TSignal>>()
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestSignalRetryMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestSignalRetryMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestSignalRetryMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalRetryMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                         ],
                         [
                             SignalTransportRole.Publisher,
                         ]);

        yield return new(p => p.Use(new TestSignalRetryMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestSignalRetryMiddleware<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestSignalMiddleware2<TSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestSignalRetryMiddleware<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalRetryMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware2<TSignal>), SignalTransportRole.Publisher),
                             (typeof(TestSignalRetryMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                             (typeof(TestSignalMiddleware<TSignal>), SignalTransportRole.Receiver),
                         ],
                         [
                             SignalTransportRole.Publisher,
                             SignalTransportRole.Receiver,
                             SignalTransportRole.Receiver,
                         ]);
    }

    [Test]
    public async Task GivenHandlerPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheSignalAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens)
                    .AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                        _ = pipeline.Use(new MutatingTestSignalMiddleware<TestSignal>(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestSignalMiddleware2<TestSignal>(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.Handle(new(0), tokens.CancellationTokens[0]);

        var signal1 = new TestSignal(0);
        var signal2 = new TestSignal(1);
        var signal3 = new TestSignal(3);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(new[] { signal1, signal2 }));
        Assert.That(observations.SignalsFromHandlers, Is.EqualTo(new[] { signal3 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPublisherPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheSignalAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.WithPipeline(pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
            _ = pipeline.Use(new MutatingTestSignalMiddleware<TestSignal>(obs, cancellationTokensToUse))
                        .Use(new MutatingTestSignalMiddleware2<TestSignal>(obs, cancellationTokensToUse));
        }).Handle(new(0), tokens.CancellationTokens[0]);

        var signal1 = new TestSignal(0);
        var signal2 = new TestSignal(1);
        var signal3 = new TestSignal(3);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(new[] { signal1, signal2 }));
        Assert.That(observations.SignalsFromHandlers, Is.EqualTo(new[] { signal3 }));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public void GivenHandlerPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(exception)
                    .AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline => pipeline.Use(new ThrowingTestSignalMiddleware<TestSignal>(exception)));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenPublisherPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestSignalMiddleware<TestSignal>(exception))).Handle(new(10)));

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

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddTransient<TestObservations>()
                    .AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline =>
                    {
                        providerFromHandlerPipelineBuild = pipeline.ServiceProvider;
                        _ = pipeline.Use(ctx =>
                        {
                            providerFromHandlerMiddleware = ctx.ServiceProvider;
                            return ctx.Next(ctx.Signal, ctx.CancellationToken);
                        });
                    });

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider
                             .GetRequiredService<ISignalPublishers>()
                             .For(TestSignal.T);

        var handler2 = scope2.ServiceProvider
                             .GetRequiredService<ISignalPublishers>()
                             .For(TestSignal.T);

        await handler1.WithPipeline(pipeline =>
        {
            providerFromPublisherPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromPublisherMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Signal, ctx.CancellationToken);
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
                return ctx.Next(ctx.Signal, ctx.CancellationToken);
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

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.WithPipeline(p => p.Use(new TestSignalMiddleware<TestSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                     .WithPipeline(p => p.Use(new TestSignalMiddleware2<TestSignal>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                     .Handle(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestSignalMiddleware<TestSignal>),
            typeof(TestSignalMiddleware2<TestSignal>),
        }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithSignal()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandlerDelegate(
                        TestSignal.T,
                        async (signal, p, cancellationToken) =>
                        {
                            await Task.Yield();
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.SignalsFromHandlers.Add(signal);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        }, pipeline => pipeline.Use(new TestSignalMiddleware<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var signal = new TestSignal(10);

        await handler.Handle(signal);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(new[] { signal }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestSignalMiddleware<TestSignal>) }));
    }

    [Test]
    public async Task GivenSyncHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithSignal()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandlerDelegate(
                        TestSignal.T,
                        (signal, p, cancellationToken) =>
                        {
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.SignalsFromHandlers.Add(signal);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        }, pipeline => pipeline.Use(new TestSignalMiddleware<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var signal = new TestSignal(10);

        await handler.Handle(signal);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(new[] { signal }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestSignalMiddleware<TestSignal>) }));
    }

    [Test]
    public async Task GivenHandlerForSignalBaseTypeWithSingleAppliedMiddleware_WhenHandlerIsCalledWithSignalSubType_MiddlewareIsCalledWithSignal()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalBaseHandler>()
                    .AddSingleton<Action<ISignalPipeline<TestSignalBase>>>(pipeline => pipeline.Use(new TestSignalMiddleware<TestSignalBase>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignalBase.T);

        var signal = new TestSignalSub(10, -1);

        await handler.Handle(signal);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(new[] { signal }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestSignalMiddleware<TestSignalBase>) }));
    }

    [Test]
    public async Task GivenHandlerRegisteredViaAssemblyScanningWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithSignal()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandlersFromExecutingAssembly()
                    .AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline => pipeline.Use(new TestSignalMiddleware<TestSignal>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var signal = new TestSignal(10);

        await handler.Handle(signal);

        Assert.That(observations.SignalsFromMiddlewares, Is.EqualTo(new[] { signal }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestSignalMiddleware<TestSignal>) }));
    }

    [Test]
    public async Task GivenHandlerAndPublisherPipeline_WhenHandlerIsCalled_TransportTypesInPipelinesAreCorrect()
    {
        var services = new ServiceCollection();
        SignalTransportType? transportTypeFromPublisher = null;
        SignalTransportType? transportTypeFromHandler = null;

        _ = services.AddSignalHandlerDelegate(
            TestSignal.T,
            async (_, _, _) => { await Task.Yield(); },
            pipeline => transportTypeFromHandler = pipeline.TransportType);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var signal = new TestSignal(10);

        await handler.WithPipeline(pipeline => transportTypeFromPublisher = pipeline.TransportType).Handle(signal);

        Assert.That(transportTypeFromPublisher, Is.EqualTo(new SignalTransportType(ConquerorConstants.InProcessTransportName, SignalTransportRole.Publisher)));
        Assert.That(transportTypeFromHandler, Is.EqualTo(new SignalTransportType(ConquerorConstants.InProcessTransportName, SignalTransportRole.Receiver)));
    }

    [Test]
    public async Task GivenHandlerAndPublisherPipeline_WhenPipelineIsBeingBuilt_MiddlewaresCanBeEnumerated()
    {
        var services = new ServiceCollection();

        _ = services.AddSignalHandlerDelegate(
            TestSignal.T,
            (_, _, _) => Task.CompletedTask,
            pipeline =>
            {
                var middleware1 = new TestSignalMiddleware<TestSignal>(new());
                var middleware2 = new TestSignalMiddleware2<TestSignal>(new());
                _ = pipeline.Use(middleware1).Use(middleware2);

                Assert.That(pipeline, Has.Count.EqualTo(2));
                Assert.That(pipeline, Is.EqualTo(new ISignalMiddleware<TestSignal>[] { middleware1, middleware2 }));
            });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        var signal = new TestSignal(10);

        await handler.WithPipeline(pipeline =>
                     {
                         var middleware1 = new TestSignalMiddleware<TestSignal>(new());
                         var middleware2 = new TestSignalMiddleware2<TestSignal>(new());
                         _ = pipeline.Use(middleware1).Use(middleware2);

                         Assert.That(pipeline, Has.Count.EqualTo(2));
                         Assert.That(pipeline, Is.EqualTo(new ISignalMiddleware<TestSignal>[] { middleware1, middleware2 }));
                     })
                     .Handle(signal);
    }

    [Test]
    public async Task GivenHandlerWithMultipleSignalTypes_WhenHandlerIsCalled_PipelineBuilderIsCalledForTheCorrectType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var builder1WasCalled = false;
        var builder2WasCalled = false;

        _ = services.AddSignalHandler<MultiTestSignalHandler>()
                    .AddSingleton<Action<ISignalPipeline<TestSignal>>>(_ => builder1WasCalled = true)
                    .AddSingleton<Action<ISignalPipeline<TestSignal2>>>(_ => builder2WasCalled = true)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ISignalPublishers>()
                      .For(TestSignal.T).Handle(new(10));

        Assert.That(builder1WasCalled, Is.True);
        Assert.That(builder2WasCalled, Is.False);

        await provider.GetRequiredService<ISignalPublishers>()
                      .For(TestSignal2.T).Handle(new());

        Assert.That(builder2WasCalled, Is.True);
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase<TSignal>(
        Action<ISignalPipeline<TSignal>>? ConfigureHandlerPipeline,
        Action<ISignalPipeline<TSignal>>? ConfigurePublisherPipeline,
        IReadOnlyCollection<(Type MiddlewareType, SignalTransportRole TransportRole)> ExpectedMiddlewareTypes,
        IReadOnlyCollection<SignalTransportRole> ExpectedTransportRolesFromPipelineBuilders)
        where TSignal : class, ISignal<TSignal>;

    [Signal]
    public sealed partial record TestSignal(int Payload);

    [Signal]
    private sealed partial record TestSignal2;

    private sealed class TestSignalHandler(TestObservations observations) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.SignalsFromHandlers.Add(signal);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
        {
            if (pipeline is ISignalPipeline<TestSignal> p)
            {
                pipeline.ServiceProvider.GetService<Action<ISignalPipeline<TestSignal>>>()?.Invoke(p);
            }
        }
    }

    [Signal]
    private partial record TestSignalBase(int PayloadBase);

    private sealed record TestSignalSub(int PayloadBase, int PayloadSub) : TestSignalBase(PayloadBase);

    private sealed class TestSignalBaseHandler(TestObservations observations) : TestSignalBase.IHandler
    {
        public async Task Handle(TestSignalBase signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.SignalsFromHandlers.Add(signal);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
        {
            if (pipeline is ISignalPipeline<TestSignalBase> p)
            {
                pipeline.ServiceProvider.GetService<Action<ISignalPipeline<TestSignalBase>>>()?.Invoke(p);
            }
        }
    }

    private sealed class MultiTestSignalHandler : TestSignal.IHandler, TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
        {
            if (pipeline is ISignalPipeline<TestSignal> p)
            {
                pipeline.ServiceProvider.GetService<Action<ISignalPipeline<TestSignal>>>()?.Invoke(p);
            }

            if (pipeline is ISignalPipeline<TestSignal2> p2)
            {
                pipeline.ServiceProvider.GetService<Action<ISignalPipeline<TestSignal2>>>()?.Invoke(p2);
            }
        }
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed class TestSignalForAssemblyScanningHandler(TestObservations observations)
        : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.SignalsFromHandlers.Add(signal);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
        {
            if (pipeline is ISignalPipeline<TestSignal> p)
            {
                pipeline.ServiceProvider.GetService<Action<ISignalPipeline<TestSignal>>>()?.Invoke(p);
            }
        }
    }

    private sealed class TestSignalMiddleware<TSignal>(TestObservations observations) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.SignalsFromMiddlewares.Add(ctx.Signal);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);
        }
    }

    private sealed class TestSignalMiddleware2<TSignal>(TestObservations observations) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.SignalsFromMiddlewares.Add(ctx.Signal);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);
        }
    }

    private sealed class TestSignalRetryMiddleware<TSignal>(TestObservations observations) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.SignalsFromMiddlewares.Add(ctx.Signal);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);
            await ctx.Next(ctx.Signal, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestSignalMiddleware<TSignal>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.SignalsFromMiddlewares.Add(ctx.Signal);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var signal = ctx.Signal;

            if (signal is TestSignal testSignal)
            {
                signal = (TSignal)(object)new TestSignal(testSignal.Payload + 1);
            }

            await ctx.Next(signal, cancellationTokensToUse.CancellationTokens[1]);
        }
    }

    private sealed class MutatingTestSignalMiddleware2<TSignal>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.SignalsFromMiddlewares.Add(ctx.Signal);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var signal = ctx.Signal;

            if (signal is TestSignal testSignal)
            {
                signal = (TSignal)(object)new TestSignal(testSignal.Payload + 2);
            }

            await ctx.Next(signal, cancellationTokensToUse.CancellationTokens[2]);
        }
    }

    private sealed class ThrowingTestSignalMiddleware<TSignal>(Exception exception) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    // only used as a marker for pipeline type check
    private sealed class DelegateSignalMiddleware<TSignal> : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx) => throw new NotSupportedException();
    }

    public sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> SignalsFromHandlers { get; } = [];

        public List<object> SignalsFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<SignalTransportType> TransportTypesFromPipelineBuilders { get; } = [];

        public List<SignalTransportType> TransportTypesFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
