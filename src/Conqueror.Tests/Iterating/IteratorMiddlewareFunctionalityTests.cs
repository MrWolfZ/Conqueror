namespace Conqueror.Tests.Iterating;

[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
[SuppressMessage(
    "Style",
    "MA0032:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1202:Elements should be ordered by access",
    Justification = "ordering makes sense here"
)]
public sealed partial class IteratorMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenClientAndHandlerPipelines_WhenHandlerIsCalled_MiddlewaresAreCalledWithIterator(
        ConquerorMiddlewareFunctionalityTestCase<TestIterator, int> testCase
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddIteratorHandler<TestIteratorHandler>()
            .AddSingleton(observations)
            .AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
            {
                if (testCase.ConfigureHandlerPipeline is null)
                {
                    return;
                }

                var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);

                testCase.ConfigureHandlerPipeline?.Invoke(pipeline);
            });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var iterator = new TestIterator(Payload: 10);

        var expectedHandlerTypesFromPipelineBuilders = testCase
            .ExpectedTransportRolesFromPipelineBuilders.Select(r =>
                r is IteratorTransportRole.Client ? null : typeof(TestIteratorHandler)
            )
            .ToList();

        using var tokenSource = new CancellationTokenSource();

        _ = await ConsumeAll(
            handler
                .WithPipeline(pipeline =>
                {
                    if (testCase.ConfigureClientPipeline is null)
                    {
                        return;
                    }

                    var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                    obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);

                    testCase.ConfigureClientPipeline?.Invoke(pipeline);
                })
                .Handle(iterator, tokenSource.Token)
        );

        Assert.That(
            observations.IteratorsFromMiddlewares,
            Is.EqualTo(Enumerable.Repeat(iterator, testCase.ExpectedMiddlewareTypes.Count))
        );
        Assert.That(
            observations.CancellationTokensFromMiddlewares,
            Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count))
        );
        Assert.That(
            observations.MiddlewareTypes,
            Is.EqualTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType))
        );
        Assert.That(
            observations.TransportTypesFromMiddlewares,
            Is.EqualTo(
                testCase.ExpectedMiddlewareTypes.Select(t => new IteratorTransportType(
                    ConquerorConstants.InProcessTransportName,
                    t.TransportRole
                ))
            )
        );
        Assert.That(
            observations.HandlerTypesFromPipelineBuilders,
            Is.EqualTo(expectedHandlerTypesFromPipelineBuilders)
        );
    }

    [Test]
    public async Task GivenClientAndHandlerPipelinesForMultipleIteratorTypes_WhenHandlerIsCalled_MiddlewaresAreCalledWithIterator()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddIteratorHandler<MultiTestIteratorHandler>()
            .AddSingleton(observations)
            .AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
            {
                var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                _ = pipeline.Use(new TestIteratorMiddleware<TestIterator, int>(obs));
            })
            .AddSingleton<Action<TestIterator2.IPipeline>>(pipeline =>
            {
                var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                _ = pipeline.Use(new TestIteratorMiddleware<TestIterator2, string>(obs));
            });

        var provider = services.BuildServiceProvider();

        var handler1 = provider
            .GetRequiredService<IIterators>()
            .For(TestIterator.T)
            .WithPipeline(pipeline =>
            {
                var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                _ = pipeline.Use(new TestIteratorMiddleware2<TestIterator, int>(obs));
            });

        var handler2 = provider
            .GetRequiredService<IIterators>()
            .For(TestIterator2.T)
            .WithPipeline(pipeline =>
            {
                var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                _ = pipeline.Use(new TestIteratorMiddleware2<TestIterator2, string>(obs));
            });

        using var tokenSource = new CancellationTokenSource();

        var iterator1 = new TestIterator(Payload: 10);
        var iterator2 = new TestIterator2(Payload: 10);

        _ = await ConsumeAll(handler1.Handle(iterator1, tokenSource.Token));

        _ = await ConsumeAll(handler2.Handle(iterator2, tokenSource.Token));

        Assert.That(
            observations.IteratorsFromMiddlewares,
            Is.EqualTo(new object[] { iterator1, iterator1, iterator2, iterator2 })
        );
        Assert.That(
            observations.MiddlewareTypes,
            Is.EqualTo(
                [
                    typeof(TestIteratorMiddleware2<TestIterator, int>),
                    typeof(TestIteratorMiddleware<TestIterator, int>),
                    typeof(TestIteratorMiddleware2<TestIterator2, string>),
                    typeof(TestIteratorMiddleware<TestIterator2, string>),
                ]
            )
        );
    }

    private static IEnumerable<TestCaseData> GenerateTestCases() =>
        GenerateTestCasesGeneric<TestIterator, int>().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear in this method which types are getting created"
    )]
    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TIterator, TItem>> GenerateTestCasesGeneric<
        TIterator,
        TItem
    >()
        where TIterator : class, IIterator<TIterator, TItem>
    {
        // no middleware
        yield return new("No middleware", ConfigureHandlerPipeline: null, ConfigureClientPipeline: null, [], []);

        // single middleware
        yield return new(
            "Single middleware on handler",
            p =>
                p.Use(
                    new TestIteratorMiddleware<TIterator, TItem>(
                        p.ServiceProvider.GetRequiredService<TestObservations>()
                    )
                ),
            ConfigureClientPipeline: null,
            [(typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server)],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Single middleware on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                    new TestIteratorMiddleware<TIterator, TItem>(
                        p.ServiceProvider.GetRequiredService<TestObservations>()
                    )
                ),
            [(typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client)],
            [IteratorTransportRole.Client]
        );

        yield return new(
            "Single middleware on both client and handler",
            p =>
                p.Use(
                    new TestIteratorMiddleware<TIterator, TItem>(
                        p.ServiceProvider.GetRequiredService<TestObservations>()
                    )
                ),
            p =>
                p.Use(
                    new TestIteratorMiddleware2<TIterator, TItem>(
                        p.ServiceProvider.GetRequiredService<TestObservations>()
                    )
                ),
            [
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // single conditional middleware
        yield return new(
            "Single conditional middleware (true) on both client and handler",
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner.Use(
                            new TestIteratorMiddleware<TIterator, TItem>(
                                p.ServiceProvider.GetRequiredService<TestObservations>()
                            )
                        )
                ),
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner.Use(
                            new TestIteratorMiddleware2<TIterator, TItem>(
                                p.ServiceProvider.GetRequiredService<TestObservations>()
                            )
                        )
                ),
            [
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Single conditional middleware (false) on both client and handler",
            p =>
                p.UseWhen(
                    _ => false,
                    inner =>
                        inner.Use(
                            new TestIteratorMiddleware<TIterator, TItem>(
                                p.ServiceProvider.GetRequiredService<TestObservations>()
                            )
                        )
                ),
            p =>
                p.UseWhen(
                    _ => false,
                    inner =>
                        inner.Use(
                            new TestIteratorMiddleware2<TIterator, TItem>(
                                p.ServiceProvider.GetRequiredService<TestObservations>()
                            )
                        )
                ),
            [],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Single nested conditional middleware (true, true) on both client and handler",
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner.UseWhen(
                            _ => true,
                            inner2 =>
                                inner2.Use(
                                    new TestIteratorMiddleware<TIterator, TItem>(
                                        p.ServiceProvider.GetRequiredService<TestObservations>()
                                    )
                                )
                        )
                ),
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner.UseWhen(
                            _ => true,
                            inner2 =>
                                inner2.Use(
                                    new TestIteratorMiddleware2<TIterator, TItem>(
                                        p.ServiceProvider.GetRequiredService<TestObservations>()
                                    )
                                )
                        )
                ),
            [
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Single nested conditional middleware (true, false) on both client and handler",
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner.UseWhen(
                            _ => false,
                            inner2 =>
                                inner2.Use(
                                    new TestIteratorMiddleware<TIterator, TItem>(
                                        p.ServiceProvider.GetRequiredService<TestObservations>()
                                    )
                                )
                        )
                ),
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner.UseWhen(
                            _ => false,
                            inner2 =>
                                inner2.Use(
                                    new TestIteratorMiddleware2<TIterator, TItem>(
                                        p.ServiceProvider.GetRequiredService<TestObservations>()
                                    )
                                )
                        )
                ),
            [],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Single nested conditional middleware (false, true) on both client and handler",
            p =>
                p.UseWhen(
                    _ => false,
                    inner =>
                        inner.UseWhen(
                            _ => true,
                            inner2 =>
                                inner2.Use(
                                    new TestIteratorMiddleware<TIterator, TItem>(
                                        p.ServiceProvider.GetRequiredService<TestObservations>()
                                    )
                                )
                        )
                ),
            p =>
                p.UseWhen(
                    _ => false,
                    inner =>
                        inner.UseWhen(
                            _ => true,
                            inner2 =>
                                inner2.Use(
                                    new TestIteratorMiddleware2<TIterator, TItem>(
                                        p.ServiceProvider.GetRequiredService<TestObservations>()
                                    )
                                )
                        )
                ),
            [],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // delegate middleware
        yield return new(
            "Delegate middleware on handler",
            p => p.Use(CreateDelegateMiddleware(p.ServiceProvider)),
            ConfigureClientPipeline: null,
            [(typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server)],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Delegate middleware on client",
            ConfigureHandlerPipeline: null,
            p => p.Use(CreateDelegateMiddleware(p.ServiceProvider)),
            [(typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client)],
            [IteratorTransportRole.Client]
        );

        yield return new(
            "Delegate middleware on both client and handler",
            p => p.Use(CreateDelegateMiddleware(p.ServiceProvider)),
            p => p.Use(CreateDelegateMiddleware(p.ServiceProvider)),
            [
                (typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // conditional delegate middleware
        yield return new(
            "Conditional delegate middleware (true) on both client and handler",
            p => p.UseWhen(_ => true, inner => inner.Use(CreateDelegateMiddleware(p.ServiceProvider))),
            p => p.UseWhen(_ => true, inner => inner.Use(CreateDelegateMiddleware(p.ServiceProvider))),
            [
                (typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Conditional delegate middleware (false) on both client and handler",
            p => p.UseWhen(_ => false, inner => inner.Use(CreateDelegateMiddleware(p.ServiceProvider))),
            p => p.UseWhen(_ => false, inner => inner.Use(CreateDelegateMiddleware(p.ServiceProvider))),
            [],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // multiple different middlewares
        yield return new(
            "Multiple different middlewares on handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            ConfigureClientPipeline: null,
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Multiple different middlewares on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
            ],
            [IteratorTransportRole.Client]
        );

        yield return new(
            "Multiple different middlewares on both client and handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            p =>
                p.Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // multiple conditional middlewares
        yield return new(
            "Multiple conditional middlewares (true) on both client and handler",
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner
                            .Use(
                                new TestIteratorMiddleware<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                            .Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                ),
            p =>
                p.UseWhen(
                    _ => true,
                    inner =>
                        inner
                            .Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                            .Use(
                                new TestIteratorMiddleware<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                ),
            [
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Multiple conditional middlewares (false) on both client and handler",
            p =>
                p.UseWhen(
                    _ => false,
                    inner =>
                        inner
                            .Use(
                                new TestIteratorMiddleware<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                            .Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                ),
            p =>
                p.UseWhen(
                    _ => false,
                    inner =>
                        inner
                            .Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                            .Use(
                                new TestIteratorMiddleware<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                ),
            [],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // mix unconditional and conditional middlewares
        yield return new(
            "Mix of unconditional and conditional (true) middlewares on both client and handler",
            p =>
                p.UseWhen(
                        _ => true,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            p =>
                p.UseWhen(
                        _ => true,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Mix of unconditional and conditional (false) middlewares on both client and handler",
            p =>
                p.UseWhen(
                        _ => false,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            p =>
                p.UseWhen(
                        _ => false,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // mix delegate and normal middleware
        yield return new(
            "Mix of delegate and normal middleware on handler",
            p =>
                p.Use(CreateDelegateMiddleware(p.ServiceProvider))
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            ConfigureClientPipeline: null,
            [
                (typeof(DelegateIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Server]
        );

        // same middleware multiple times
        yield return new(
            "Same middleware multiple times on handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            ConfigureClientPipeline: null,
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Same middleware multiple times on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
            ],
            [IteratorTransportRole.Client]
        );

        // added, then removed
        yield return new(
            "Middleware added then removed on handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            ConfigureClientPipeline: null,
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Middleware added then removed on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
            ],
            [IteratorTransportRole.Client]
        );

        // added conditional, then removed
        yield return new(
            "Conditional middleware added then removed with true condition on both client and handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .UseWhen(
                        _ => true,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .UseWhen(
                        _ => true,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        yield return new(
            "Conditional middleware added then removed with false condition on both client and handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .UseWhen(
                        _ => false,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .UseWhen(
                        _ => false,
                        inner =>
                            inner.Use(
                                new TestIteratorMiddleware2<TIterator, TItem>(
                                    p.ServiceProvider.GetRequiredService<TestObservations>()
                                )
                            )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // multiple times added, then removed
        yield return new(
            "Multiple middlewares added then one removed on handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            ConfigureClientPipeline: null,
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Multiple middlewares added then one removed on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware2<TIterator, TItem>>(),
            [
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
            ],
            [IteratorTransportRole.Client]
        );

        // added on client, added and removed in handler
        yield return new(
            "Middleware added on client and added then removed on handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware<TIterator, TItem>>(),
            p =>
                p.Use(
                    new TestIteratorMiddleware<TIterator, TItem>(
                        p.ServiceProvider.GetRequiredService<TestObservations>()
                    )
                ),
            [(typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client)],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        // added, then removed, then added again
        yield return new(
            "Middleware added then removed then added again on handler",
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware<TIterator, TItem>>()
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            ConfigureClientPipeline: null,
            [(typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server)],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Middleware added then removed then added again on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Without<TestIteratorMiddleware<TIterator, TItem>>()
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [(typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client)],
            [IteratorTransportRole.Client]
        );

        // retry middlewares
        yield return new(
            "Retry middleware with middlewares after it on handler",
            p =>
                p.Use(
                        new TestIteratorRetryMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            ConfigureClientPipeline: null,
            [
                (typeof(TestIteratorRetryMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Server]
        );

        yield return new(
            "Retry middleware with middlewares after it on client",
            ConfigureHandlerPipeline: null,
            p =>
                p.Use(
                        new TestIteratorRetryMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorRetryMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
            ],
            [IteratorTransportRole.Client]
        );

        yield return new(
            "Retry middleware with middlewares after it on client and handler",
            p =>
                p.Use(
                        new TestIteratorRetryMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            p =>
                p.Use(
                        new TestIteratorRetryMiddleware<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                    .Use(
                        new TestIteratorMiddleware2<TIterator, TItem>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    ),
            [
                (typeof(TestIteratorRetryMiddleware<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorRetryMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware2<TIterator, TItem>), IteratorTransportRole.Client),
                (typeof(TestIteratorRetryMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
                (typeof(TestIteratorMiddleware<TIterator, TItem>), IteratorTransportRole.Server),
            ],
            [IteratorTransportRole.Client, IteratorTransportRole.Server]
        );

        static IteratorMiddlewareFn<TIterator, TItem> CreateDelegateMiddleware(IServiceProvider serviceProvider)
        {
            return ExecuteMiddleware;

            async IAsyncEnumerable<TItem> ExecuteMiddleware(IteratorMiddlewareContext<TIterator, TItem> ctx)
            {
                await Task.Yield();
                var observations = serviceProvider.GetRequiredService<TestObservations>();
                observations.MiddlewareTypes.Add(typeof(DelegateIteratorMiddleware<TIterator, TItem>));
                observations.IteratorsFromMiddlewares.Add(ctx.Iterator);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
                {
                    yield return item;
                }
            }
        }
    }

    [Test]
    public async Task GivenHandlerPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheIteratorAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse
        {
            CancellationTokens =
            {
                new(canceled: false),
                new(canceled: false),
                new(canceled: false),
                new(canceled: false),
                new(canceled: false),
            },
        };

        _ = services
            .AddIteratorHandler<TestIteratorHandler>()
            .AddSingleton(observations)
            .AddSingleton(tokens)
            .AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
            {
                var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                _ = pipeline
                    .Use(new MutatingTestIteratorMiddleware<TestIterator, int>(obs, cancellationTokensToUse))
                    .Use(new MutatingTestIteratorMiddleware2<TestIterator, int>(obs, cancellationTokensToUse));
            });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var items = await ConsumeAll(handler.Handle(new(Payload: 0), tokens.CancellationTokens[0]));

        var iterator1 = new TestIterator(Payload: 0);
        var iterator2 = new TestIterator(Payload: 1);
        var iterator3 = new TestIterator(Payload: 3);

        Assert.That(observations.IteratorsFromMiddlewares, Is.EqualTo([iterator1, iterator2]));
        Assert.That(observations.IteratorsFromHandlers, Is.EqualTo([iterator3]));

        Assert.That(
            observations.CancellationTokensFromMiddlewares,
            Is.EqualTo(tokens.CancellationTokens.Take(count: 2))
        );
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo([tokens.CancellationTokens[2]]));

        Assert.That(items, Is.EqualTo([11, 12, 13]));
    }

    [Test]
    public async Task GivenClientPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheIteratorAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse
        {
            CancellationTokens =
            {
                new(canceled: false),
                new(canceled: false),
                new(canceled: false),
                new(canceled: false),
                new(canceled: false),
            },
        };

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations).AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var items = await ConsumeAll(
            handler
                .WithPipeline(pipeline =>
                {
                    var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                    var cancellationTokensToUse =
                        pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                    _ = pipeline
                        .Use(new MutatingTestIteratorMiddleware<TestIterator, int>(obs, cancellationTokensToUse))
                        .Use(new MutatingTestIteratorMiddleware2<TestIterator, int>(obs, cancellationTokensToUse));
                })
                .Handle(new(Payload: 0), tokens.CancellationTokens[0])
        );

        var iterator1 = new TestIterator(Payload: 0);
        var iterator2 = new TestIterator(Payload: 1);
        var iterator3 = new TestIterator(Payload: 3);

        Assert.That(observations.IteratorsFromMiddlewares, Is.EqualTo([iterator1, iterator2]));
        Assert.That(observations.IteratorsFromHandlers, Is.EqualTo([iterator3]));

        Assert.That(
            observations.CancellationTokensFromMiddlewares,
            Is.EqualTo(tokens.CancellationTokens.Take(count: 2))
        );
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo([tokens.CancellationTokens[2]]));

        Assert.That(items, Is.EqualTo([11, 12, 13]));
    }

    [Test]
    public void GivenHandlerPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services
            .AddIteratorHandler<TestIteratorHandler>()
            .AddSingleton(exception)
            .AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
                pipeline.Use(new ThrowingTestIteratorMiddleware<TestIterator, int>(exception))
            );

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var thrownException = Assert.ThrowsAsync<Exception>(async () =>
            await ConsumeAll(handler.Handle(new(Payload: 10), CancellationToken.None))
        );

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenClientPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var thrownException = Assert.ThrowsAsync<Exception>(async () =>
            await ConsumeAll(
                handler
                    .WithPipeline(p => p.Use(new ThrowingTestIteratorMiddleware<TestIterator, int>(exception)))
                    .Handle(new(Payload: 10), CancellationToken.None)
            )
        );

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandlerWithMiddlewares_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromResolutionScope()
    {
        var services = new ServiceCollection();

        IServiceProvider? providerFromHandlerPipelineBuild = null;
        IServiceProvider? providerFromHandlerMiddleware = null;
        IServiceProvider? providerFromClientPipelineBuild = null;
        IServiceProvider? providerFromClientMiddleware = null;

        _ = services
            .AddIteratorHandler<TestIteratorHandler>()
            .AddTransient<TestObservations>()
            .AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
            {
                providerFromHandlerPipelineBuild = pipeline.ServiceProvider;
                _ = pipeline.Use(ExecuteMiddleware);

                async IAsyncEnumerable<int> ExecuteMiddleware(IteratorMiddlewareContext<TestIterator, int> ctx)
                {
                    await Task.Yield();
                    providerFromHandlerMiddleware = ctx.ServiceProvider;

                    await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
                    {
                        yield return item;
                    }
                }
            });

        var provider = services.BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IIterators>().For(TestIterator.T);

        var handler2 = scope2.ServiceProvider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(
            handler1
                .WithPipeline(pipeline =>
                {
                    providerFromClientPipelineBuild = pipeline.ServiceProvider;
                    _ = pipeline.Use(ExecuteMiddleware);

                    async IAsyncEnumerable<int> ExecuteMiddleware(IteratorMiddlewareContext<TestIterator, int> ctx)
                    {
                        await Task.Yield();
                        providerFromClientMiddleware = ctx.ServiceProvider;

                        await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
                        {
                            yield return item;
                        }
                    }
                })
                .Handle(new(Payload: 10), CancellationToken.None)
        );

        Assert.That(providerFromHandlerPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope1.ServiceProvider));

        _ = await ConsumeAll(
            handler2
                .WithPipeline(pipeline =>
                {
                    providerFromClientPipelineBuild = pipeline.ServiceProvider;
                    _ = pipeline.Use(ExecuteMiddleware);

                    async IAsyncEnumerable<int> ExecuteMiddleware(IteratorMiddlewareContext<TestIterator, int> ctx)
                    {
                        await Task.Yield();
                        providerFromClientMiddleware = ctx.ServiceProvider;

                        await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
                        {
                            yield return item;
                        }
                    }
                })
                .Handle(new(Payload: 10), CancellationToken.None)
        );

        Assert.That(providerFromHandlerPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerPipelineBuild, Is.Not.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope2.ServiceProvider));
    }

    [Test]
    public async Task GivenMultipleClientPipelineConfigurations_WhenHandlerIsCalled_PipelinesAreExecutedInOrder()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        _ = await ConsumeAll(
            handler
                .WithPipeline(p =>
                    p.Use(
                        new TestIteratorMiddleware<TestIterator, int>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                )
                .WithPipeline(p =>
                    p.Use(
                        new TestIteratorMiddleware2<TestIterator, int>(
                            p.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    )
                )
                .Handle(new(Payload: 10), CancellationToken.None)
        );

        Assert.That(
            observations.MiddlewareTypes,
            Is.EqualTo(
                [typeof(TestIteratorMiddleware<TestIterator, int>), typeof(TestIteratorMiddleware2<TestIterator, int>)]
            )
        );
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithIterator()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddIteratorHandlerDelegate(
                TestIterator.T,
                HandleIterator,
                pipeline =>
                {
                    var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                    obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);
                    _ = pipeline.Use(new TestIteratorMiddleware<TestIterator, int>(obs));
                }
            )
            .AddSingleton(observations);

        static async IAsyncEnumerable<int> HandleIterator(
            TestIterator iterator,
            IServiceProvider p,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            var obs = p.GetRequiredService<TestObservations>();
            obs.IteratorsFromHandlers.Add(iterator);
            obs.CancellationTokensFromHandlers.Add(cancellationToken);

            yield return 11;
            yield return 12;
            yield return 13;
        }

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var iterator = new TestIterator(Payload: 10);

        _ = await ConsumeAll(handler.Handle(iterator, CancellationToken.None));

        Assert.That(observations.IteratorsFromMiddlewares, Is.EqualTo([iterator]));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo([typeof(TestIteratorMiddleware<TestIterator, int>)]));
        Assert.That(observations.HandlerTypesFromPipelineBuilders, Is.EqualTo(new Type?[] { null }));
        Assert.That(observations.IteratorsFromMiddlewares, Is.EqualTo(new object[] { iterator }));
    }

    [Test]
    public async Task GivenHandlerForIteratorBaseTypeWithSingleAppliedMiddleware_WhenHandlerIsCalledWithIteratorSubType_MiddlewareIsCalledWithIterator()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddIteratorHandler<TestIteratorBaseHandler>()
            .AddSingleton<Action<TestIteratorBase.IPipeline>>(pipeline =>
                pipeline.Use(new TestIteratorMiddleware<TestIteratorBase, int>(observations))
            )
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIteratorBase.T);

        var iterator = new TestIteratorSub(PayloadBase: 10, PayloadSub: -1);

        _ = await ConsumeAll(handler.Handle(iterator, CancellationToken.None));

        Assert.That(observations.IteratorsFromMiddlewares, Is.EqualTo([iterator]));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo([typeof(TestIteratorMiddleware<TestIteratorBase, int>)]));
    }

    [Test]
    public async Task GivenHandlerRegisteredViaAssemblyScanningWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithIterator()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddIteratorHandlersFromAssembly(typeof(TestIterator).Assembly)
            .AddSingleton<Action<TestIterator.IPipeline>>(pipeline =>
                pipeline.Use(new TestIteratorMiddleware<TestIterator, int>(observations))
            )
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var iterator = new TestIterator(Payload: 10);

        _ = await ConsumeAll(handler.Handle(iterator, CancellationToken.None));

        Assert.That(observations.IteratorsFromMiddlewares, Is.EqualTo([iterator]));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo([typeof(TestIteratorMiddleware<TestIterator, int>)]));
    }

    [Test]
    public async Task GivenHandlerAndClientPipeline_WhenPipelineIsBeingBuilt_MiddlewaresCanBeEnumerated()
    {
        var services = new ServiceCollection();

        _ = services.AddIteratorHandlerDelegate(
            TestIterator.T,
            (_, _, _) => AsyncEnumerableHelper.Of(11, 12, 13),
            pipeline =>
            {
                var middleware1 = new TestIteratorMiddleware<TestIterator, int>(new());
                var middleware2 = new TestIteratorMiddleware2<TestIterator, int>(new());
                _ = pipeline.Use(middleware1).Use(middleware2);

                Assert.That(pipeline, Has.Count.EqualTo(expected: 2));
                Assert.That(
                    pipeline,
                    Is.EqualTo(new IIteratorMiddleware<TestIterator, int>[] { middleware1, middleware2 })
                );
            }
        );

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var iterator = new TestIterator(Payload: 10);

        _ = await ConsumeAll(
            handler
                .WithPipeline(pipeline =>
                {
                    var middleware1 = new TestIteratorMiddleware<TestIterator, int>(new());
                    var middleware2 = new TestIteratorMiddleware2<TestIterator, int>(new());
                    _ = pipeline.Use(middleware1).Use(middleware2);

                    Assert.That(pipeline, Has.Count.EqualTo(expected: 2));
                    Assert.That(
                        pipeline,
                        Is.EqualTo(new IIteratorMiddleware<TestIterator, int>[] { middleware1, middleware2 })
                    );
                })
                .Handle(iterator, CancellationToken.None)
        );
    }

    [Test]
    public async Task GivenHandlerAndClientPipelineWithConditionalMiddlewares_WhenPipelineIsBeingExecuted_PredicateGetsPassedSameContextAsMiddleware()
    {
        var services = new ServiceCollection();

        IteratorMiddlewareContext<TestIterator, int>? seenContextInPredicateOnHandler = null;
        IteratorMiddlewareContext<TestIterator, int>? seenContextInPredicateOnClient = null;

        IteratorMiddlewareContext<TestIterator, int>? seenContextInMiddlewareOnHandler = null;
        IteratorMiddlewareContext<TestIterator, int>? seenContextInMiddlewareOnClient = null;

        _ = services.AddIteratorHandlerDelegate(
            TestIterator.T,
            (_, _, _) => AsyncEnumerableHelper.Of(11, 12, 13),
            pipeline =>
                pipeline.UseWhen(
                    ctx =>
                    {
                        seenContextInPredicateOnHandler = ctx;

                        return true;
                    },
                    inner => inner.Use(ExecuteMiddleware)
                )
        );

        async IAsyncEnumerable<int> ExecuteMiddleware(IteratorMiddlewareContext<TestIterator, int> ctx)
        {
            await Task.Yield();
            seenContextInMiddlewareOnHandler = ctx;

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        var iterator = new TestIterator(Payload: 10);

        _ = await ConsumeAll(
            handler
                .WithPipeline(pipeline =>
                    pipeline.UseWhen(
                        ctx =>
                        {
                            seenContextInPredicateOnClient = ctx;

                            return true;
                        },
                        inner => inner.Use(ExecuteMiddleware2)
                    )
                )
                .Handle(iterator, CancellationToken.None)
        );

        async IAsyncEnumerable<int> ExecuteMiddleware2(IteratorMiddlewareContext<TestIterator, int> ctx)
        {
            await Task.Yield();
            seenContextInMiddlewareOnClient = ctx;

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }

        Assert.That(seenContextInPredicateOnClient, Is.EqualTo(seenContextInMiddlewareOnClient));
        Assert.That(seenContextInPredicateOnHandler, Is.EqualTo(seenContextInMiddlewareOnHandler));
    }

    private static async Task<List<T>> ConsumeAll<T>(
        IAsyncEnumerable<T> enumerable,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<T>();
        await foreach (var item in enumerable.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }

        return list;
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase<TIterator, TItem>(
        string Name,
        Action<IIteratorPipeline<TIterator, TItem>>? ConfigureHandlerPipeline,
        Action<IIteratorPipeline<TIterator, TItem>>? ConfigureClientPipeline,
        IReadOnlyCollection<(Type MiddlewareType, IteratorTransportRole TransportRole)> ExpectedMiddlewareTypes,
        IReadOnlyCollection<IteratorTransportRole> ExpectedTransportRolesFromPipelineBuilders
    )
        where TIterator : class, IIterator<TIterator, TItem>;

    [Iterator<int>]
    public sealed partial record TestIterator(int Payload);

    private sealed partial class TestIteratorHandler(TestObservations observations) : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            observations.IteratorsFromHandlers.Add(iterator);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);

            yield return 11;
            yield return 12;
            yield return 13;
        }

        public static void ConfigurePipeline(TestIterator.IPipeline pipeline) =>
            pipeline.ServiceProvider.GetService<Action<TestIterator.IPipeline>>()?.Invoke(pipeline);
    }

    [Iterator<string>]
    public sealed partial record TestIterator2(int Payload);

    private sealed partial class MultiTestIteratorHandler(TestObservations observations)
        : TestIterator.IHandler,
            TestIterator2.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            observations.IteratorsFromHandlers.Add(iterator);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);

            yield return 11;
            yield return 12;
            yield return 13;
        }

        public async IAsyncEnumerable<string> Handle(
            TestIterator2 iterator,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            observations.IteratorsFromHandlers.Add(iterator);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);

            yield return "a";
            yield return "b";
            yield return "c";
        }

        public static void ConfigurePipeline(TestIterator.IPipeline pipeline) =>
            pipeline.ServiceProvider.GetService<Action<TestIterator.IPipeline>>()?.Invoke(pipeline);

        public static void ConfigurePipeline(TestIterator2.IPipeline pipeline) =>
            pipeline.ServiceProvider.GetService<Action<TestIterator2.IPipeline>>()?.Invoke(pipeline);
    }

    [Iterator<int>]
    private partial record TestIteratorBase(int PayloadBase);

    private sealed record TestIteratorSub(int PayloadBase, int PayloadSub) : TestIteratorBase(PayloadBase);

    private sealed partial class TestIteratorBaseHandler(TestObservations observations) : TestIteratorBase.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIteratorBase iterator,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            observations.IteratorsFromHandlers.Add(iterator);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);

            yield return iterator.PayloadBase + 1;
        }

        public static void ConfigurePipeline(TestIteratorBase.IPipeline pipeline) =>
            pipeline.ServiceProvider.GetService<Action<TestIteratorBase.IPipeline>>()?.Invoke(pipeline);
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed partial class TestIteratorForAssemblyScanningHandler(TestObservations observations)
        : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            observations.IteratorsFromHandlers.Add(iterator);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);

            yield return 11;
            yield return 12;
            yield return 13;
        }

        public static void ConfigurePipeline(TestIterator.IPipeline pipeline) =>
            pipeline.ServiceProvider.GetService<Action<TestIterator.IPipeline>>()?.Invoke(pipeline);
    }

    private sealed class TestIteratorMiddleware<TIterator, TItem>(TestObservations observations)
        : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.IteratorsFromMiddlewares.Add(ctx.Iterator);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestIteratorMiddleware2<TIterator, TItem>(TestObservations observations)
        : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.IteratorsFromMiddlewares.Add(ctx.Iterator);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestIteratorRetryMiddleware<TIterator, TItem>(TestObservations observations)
        : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.IteratorsFromMiddlewares.Add(ctx.Iterator);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class MutatingTestIteratorMiddleware<TIterator, TItem>(
        TestObservations observations,
        CancellationTokensToUse cancellationTokensToUse
    ) : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.IteratorsFromMiddlewares.Add(ctx.Iterator);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var iterator = ctx.Iterator;

            if (iterator is TestIterator testIterator)
            {
                iterator = (TIterator)(object)new TestIterator(testIterator.Payload + 1);
            }

            await foreach (var item in ctx.Next(iterator, cancellationTokensToUse.CancellationTokens[1]))
            {
                yield return item;
            }
        }
    }

    private sealed class MutatingTestIteratorMiddleware2<TIterator, TItem>(
        TestObservations observations,
        CancellationTokensToUse cancellationTokensToUse
    ) : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.IteratorsFromMiddlewares.Add(ctx.Iterator);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var iterator = ctx.Iterator;

            if (iterator is TestIterator testIterator)
            {
                iterator = (TIterator)(object)new TestIterator(testIterator.Payload + 2);
            }

            await foreach (var item in ctx.Next(iterator, cancellationTokensToUse.CancellationTokens[2]))
            {
                yield return item;
            }
        }
    }

    private sealed class ThrowingTestIteratorMiddleware<TIterator, TItem>(Exception exception)
        : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();

            throw exception;
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162 // Unreachable code detected
        }
    }

    // only used as a marker for pipeline type check
    private sealed class DelegateIteratorMiddleware<TIterator, TItem> : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx) =>
            throw new NotSupportedException();
    }

    public sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> IteratorsFromHandlers { get; } = [];

        public List<object> IteratorsFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<Type?> HandlerTypesFromPipelineBuilders { get; } = [];

        public List<IteratorTransportType> TransportTypesFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }

    private static class AsyncEnumerableHelper
    {
        public static async IAsyncEnumerable<TItem> Of<TItem>(params TItem[] items)
        {
            await Task.Yield();

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
}
