namespace Conqueror.Tests.Messaging;

public sealed partial class MessageMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenClientAndHandlerPipelines_WhenHandlerIsCalled_MiddlewaresAreCalledWithMessage(
        ConquerorMiddlewareFunctionalityTestCase<TestMessage, TestMessageResponse> testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
                    {
                        if (testCase.ConfigureHandlerPipeline is null)
                        {
                            return;
                        }

                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);
                        obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);

                        testCase.ConfigureHandlerPipeline?.Invoke(pipeline);
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var expectedTransportTypesFromPipelineBuilders = testCase.ExpectedTransportRolesFromPipelineBuilders
                                                                 .Select(r => new MessageTransportType(ConquerorConstants.InProcessTransportName, r))
                                                                 .ToList();

        var expectedHandlerTypesFromPipelineBuilders = testCase.ExpectedTransportRolesFromPipelineBuilders
                                                               .Select(r => r == MessageTransportRole.Sender ? null : typeof(TestMessageHandler))
                                                               .ToList();

        using var tokenSource = new CancellationTokenSource();

        var message = new TestMessage(10);

        _ = await handler.WithPipeline(pipeline =>
        {
            if (testCase.ConfigureClientPipeline is null)
            {
                return;
            }

            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            obs.TransportTypesFromPipelineBuilders.Add(pipeline.TransportType);
            obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);

            testCase.ConfigureClientPipeline?.Invoke(pipeline);
        }).Handle(message, tokenSource.Token);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(Enumerable.Repeat(message, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares,
                    Is.EqualTo(testCase.ExpectedMiddlewareTypes
                                       .Select(t => new MessageTransportType(ConquerorConstants.InProcessTransportName, t.TransportRole))));
        Assert.That(observations.TransportTypesFromPipelineBuilders, Is.EqualTo(expectedTransportTypesFromPipelineBuilders));
        Assert.That(observations.HandlerTypesFromPipelineBuilders, Is.EqualTo(expectedHandlerTypesFromPipelineBuilders));
    }

    [Test]
    [TestCaseSource(nameof(GenerateTestCasesWithoutResponse))]
    public async Task GivenClientAndHandlerPipelinesWithoutResponse_WhenHandlerIsCalled_MiddlewaresAreCalledWithMessage(
        ConquerorMiddlewareFunctionalityTestCase<TestMessageWithoutResponse, UnitMessageResponse> testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<TestMessageWithoutResponseHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<TestMessageWithoutResponse.IPipeline>>(pipeline => testCase.ConfigureHandlerPipeline?.Invoke(pipeline));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessageWithoutResponse.T);

        using var tokenSource = new CancellationTokenSource();

        var message = new TestMessageWithoutResponse(10);

        await handler.WithPipeline(pipeline => testCase.ConfigureClientPipeline?.Invoke(pipeline)).Handle(message, tokenSource.Token);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(Enumerable.Repeat(message, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EqualTo(testCase.ExpectedMiddlewareTypes.Select(t => new MessageTransportType(ConquerorConstants.InProcessTransportName, t.TransportRole))));
    }

    [Test]
    public async Task GivenClientAndHandlerPipelinesForMultipleMessageTypes_WhenHandlerIsCalled_MiddlewaresAreCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<MultiTestMessageHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(obs));
                    })
                    .AddSingleton<Action<TestMessageWithoutResponse.IPipeline>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        _ = pipeline.Use(new TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>(obs));
                    });

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IMessageSenders>()
                               .For(TestMessage.T)
                               .WithPipeline(pipeline =>
                               {
                                   var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                                   _ = pipeline.Use(new TestMessageMiddleware2<TestMessage, TestMessageResponse>(obs));
                               });

        var handler2 = provider.GetRequiredService<IMessageSenders>()
                               .For(TestMessageWithoutResponse.T)
                               .WithPipeline(pipeline =>
                               {
                                   var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                                   _ = pipeline.Use(new TestMessageMiddleware2<TestMessageWithoutResponse, UnitMessageResponse>(obs));
                               });

        using var tokenSource = new CancellationTokenSource();

        var message1 = new TestMessage(10);
        var message2 = new TestMessageWithoutResponse(10);

        _ = await handler1.Handle(message1, tokenSource.Token);

        await handler2.Handle(message2, tokenSource.Token);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new object[] { message1, message1, message2, message2 }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestMessageMiddleware2<TestMessage, TestMessageResponse>),
            typeof(TestMessageMiddleware<TestMessage, TestMessageResponse>),

            typeof(TestMessageMiddleware2<TestMessageWithoutResponse, UnitMessageResponse>),
            typeof(TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>),
        }));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TestMessage, TestMessageResponse>> GenerateTestCases()
        => GenerateTestCasesGeneric<TestMessage, TestMessageResponse>();

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TestMessageWithoutResponse, UnitMessageResponse>> GenerateTestCasesWithoutResponse()
        => GenerateTestCasesGeneric<TestMessageWithoutResponse, UnitMessageResponse>();

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase<TMessage, TResponse>> GenerateTestCasesGeneric<TMessage, TResponse>()
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        // no middleware
        yield return new(null,
                         null,
                         [],
                         []);

        // single middleware
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Sender,
                             MessageTransportRole.Receiver,
                         ]);

        // delegate middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateMessageMiddleware<TMessage, TResponse>));
                             observations.MessagesFromMiddlewares.Add(ctx.Message);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Message, ctx.CancellationToken);
                         }),
                         null,
                         [
                             (typeof(DelegateMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateMessageMiddleware<TMessage, TResponse>));
                             observations.MessagesFromMiddlewares.Add(ctx.Message);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Message, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateMessageMiddleware<TMessage, TResponse>));
                             observations.MessagesFromMiddlewares.Add(ctx.Message);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Message, ctx.CancellationToken);
                         }),
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateMessageMiddleware<TMessage, TResponse>));
                             observations.MessagesFromMiddlewares.Add(ctx.Message);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Message, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(DelegateMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Sender,
                             MessageTransportRole.Receiver,
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Sender,
                             MessageTransportRole.Receiver,
                         ]);

        // mix delegate and normal middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateMessageMiddleware<TMessage, TResponse>));
                             observations.MessagesFromMiddlewares.Add(ctx.Message);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Message, ctx.CancellationToken);
                         }).Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(DelegateMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware2<TMessage, TResponse>>(),
                         null,
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware2<TMessage, TResponse>>(),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware2<TMessage, TResponse>>(),
                         null,
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware2<TMessage, TResponse>>(),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware<TMessage, TResponse>>(),
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                             MessageTransportRole.Receiver,
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware<TMessage, TResponse>>()
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestMessageMiddleware<TMessage, TResponse>>()
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestMessageRetryMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestMessageRetryMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Receiver,
                         ]);

        yield return new(null,
                         p => p.Use(new TestMessageRetryMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageRetryMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                         ],
                         [
                             MessageTransportRole.Sender,
                         ]);

        yield return new(p => p.Use(new TestMessageRetryMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestMessageRetryMiddleware<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestMessageMiddleware2<TMessage, TResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestMessageRetryMiddleware<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageRetryMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware2<TMessage, TResponse>), MessageTransportRole.Sender),
                             (typeof(TestMessageRetryMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                             (typeof(TestMessageMiddleware<TMessage, TResponse>), MessageTransportRole.Receiver),
                         ],
                         [
                             MessageTransportRole.Sender,
                             MessageTransportRole.Receiver,
                             MessageTransportRole.Receiver,
                         ]);
    }

    [Test]
    public async Task GivenHandlerPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheMessageAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens)
                    .AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                        _ = pipeline.Use(new MutatingTestMessageMiddleware<TestMessage, TestMessageResponse>(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestMessageMiddleware2<TestMessage, TestMessageResponse>(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var response = await handler.Handle(new(0), tokens.CancellationTokens[0]);

        var message1 = new TestMessage(0);
        var message2 = new TestMessage(1);
        var message3 = new TestMessage(3);

        var response1 = new TestMessageResponse(0);
        var response2 = new TestMessageResponse(1);
        var response3 = new TestMessageResponse(3);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message1, message2 }));
        Assert.That(observations.MessagesFromHandlers, Is.EqualTo(new[] { message3 }));

        Assert.That(observations.ResponsesFromMiddlewares, Is.EqualTo(new[] { response1, response2 }));
        Assert.That(response, Is.EqualTo(response3));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenClientPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheMessageAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var response = await handler.WithPipeline(pipeline =>
        {
            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
            var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
            _ = pipeline.Use(new MutatingTestMessageMiddleware<TestMessage, TestMessageResponse>(obs, cancellationTokensToUse))
                        .Use(new MutatingTestMessageMiddleware2<TestMessage, TestMessageResponse>(obs, cancellationTokensToUse));
        }).Handle(new(0), tokens.CancellationTokens[0]);

        var message1 = new TestMessage(0);
        var message2 = new TestMessage(1);
        var message3 = new TestMessage(3);

        var response1 = new TestMessageResponse(0);
        var response2 = new TestMessageResponse(1);
        var response3 = new TestMessageResponse(3);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message1, message2 }));
        Assert.That(observations.MessagesFromHandlers, Is.EqualTo(new[] { message3 }));

        Assert.That(observations.ResponsesFromMiddlewares, Is.EqualTo(new[] { response1, response2 }));
        Assert.That(response, Is.EqualTo(response3));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public void GivenHandlerPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddSingleton(exception)
                    .AddSingleton<Action<TestMessage.IPipeline>>(pipeline => pipeline.Use(new ThrowingTestMessageMiddleware<TestMessage, TestMessageResponse>(exception)));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenClientPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestMessageMiddleware<TestMessage, TestMessageResponse>(exception))).Handle(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandlerWithMiddlewares_WhenMiddlewareIsExecuted_ServiceProviderInContextAndPipelineConfigurationIsFromResolutionScope()
    {
        var services = new ServiceCollection();

        IServiceProvider? providerFromHandlerPipelineBuild = null;
        IServiceProvider? providerFromHandlerMiddleware = null;
        IServiceProvider? providerFromClientPipelineBuild = null;
        IServiceProvider? providerFromClientMiddleware = null;

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddTransient<TestObservations>()
                    .AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
                    {
                        providerFromHandlerPipelineBuild = pipeline.ServiceProvider;
                        _ = pipeline.Use(ctx =>
                        {
                            providerFromHandlerMiddleware = ctx.ServiceProvider;
                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        });
                    });

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider
                             .GetRequiredService<IMessageSenders>()
                             .For(TestMessage.T);

        var handler2 = scope2.ServiceProvider
                             .GetRequiredService<IMessageSenders>()
                             .For(TestMessage.T);

        _ = await handler1.WithPipeline(pipeline =>
        {
            providerFromClientPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Message, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromHandlerPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope1.ServiceProvider));

        _ = await handler2.WithPipeline(pipeline =>
        {
            providerFromClientPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Message, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromHandlerPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope2.ServiceProvider));
    }

    [Test]
    public async Task GivenMultipleClientPipelineConfigurations_WhenHandlerIsCalled_PipelinesAreExecutedInOrder()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        _ = await handler.WithPipeline(p => p.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .WithPipeline(p => p.Use(new TestMessageMiddleware2<TestMessage, TestMessageResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .Handle(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestMessageMiddleware<TestMessage, TestMessageResponse>),
            typeof(TestMessageMiddleware2<TestMessage, TestMessageResponse>),
        }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandlerDelegate(
                        TestMessage.T,
                        async (message, p, cancellationToken) =>
                        {
                            await Task.Yield();
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.MessagesFromHandlers.Add(message);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                            return new(message.Payload + 1);
                        }, pipeline =>
                        {
                            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                            obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);
                            _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(obs));
                        })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var message = new TestMessage(10);

        _ = await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessage, TestMessageResponse>) }));
        Assert.That(observations.HandlerTypesFromPipelineBuilders, Is.EqualTo(new Type?[] { null }));
    }

    [Test]
    public async Task GivenSyncHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandlerDelegate(
                        TestMessage.T,
                        (message, p, cancellationToken) =>
                        {
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.MessagesFromHandlers.Add(message);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                            return new(message.Payload + 1);
                        }, pipeline =>
                        {
                            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                            obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);
                            _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(obs));
                        })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var message = new TestMessage(10);

        _ = await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessage, TestMessageResponse>) }));
        Assert.That(observations.HandlerTypesFromPipelineBuilders, Is.EqualTo(new Type?[] { null }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithoutResponseWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        async (message, p, cancellationToken) =>
                        {
                            await Task.Yield();
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.MessagesFromHandlers.Add(message);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        }, pipeline =>
                        {
                            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                            obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);
                            _ = pipeline.Use(new TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>(obs));
                        })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessageWithoutResponse.T);

        var message = new TestMessageWithoutResponse(10);

        await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>) }));
        Assert.That(observations.HandlerTypesFromPipelineBuilders, Is.EqualTo(new Type?[] { null }));
    }

    [Test]
    public async Task GivenSyncHandlerDelegateWithoutResponseWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (message, p, cancellationToken) =>
                        {
                            var obs = p.GetRequiredService<TestObservations>();
                            obs.MessagesFromHandlers.Add(message);
                            obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        }, pipeline =>
                        {
                            var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                            obs.HandlerTypesFromPipelineBuilders.Add(pipeline.HandlerType);
                            _ = pipeline.Use(new TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>(obs));
                        })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessageWithoutResponse.T);

        var message = new TestMessageWithoutResponse(10);

        await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>) }));
        Assert.That(observations.HandlerTypesFromPipelineBuilders, Is.EqualTo(new Type?[] { null }));
    }

    [Test]
    public async Task GivenHandlerForMessageBaseTypeWithSingleAppliedMiddleware_WhenHandlerIsCalledWithMessageSubType_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<TestMessageBaseHandler>()
                    .AddSingleton<Action<TestMessageBase.IPipeline>>(pipeline => pipeline.Use(new TestMessageMiddleware<TestMessageBase, TestMessageResponse>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessageBase.T);

        var message = new TestMessageSub(10, -1);

        _ = await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessageBase, TestMessageResponse>) }));
    }

    [Test]
    public async Task GivenHandlerRegisteredViaAssemblyScanningWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandlersFromAssembly(typeof(TestMessage).Assembly)
                    .AddSingleton<Action<TestMessage.IPipeline>>(pipeline => pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var message = new TestMessage(10);

        _ = await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessage, TestMessageResponse>) }));
    }

    [Test]
    public async Task GivenHandlerWithoutResponseRegisteredViaAssemblyScanningWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandlersFromAssembly(typeof(TestMessageWithoutResponse).Assembly)
                    .AddSingleton<Action<TestMessageWithoutResponse.IPipeline>>(pipeline => pipeline.Use(new TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>(observations)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessageWithoutResponse.T);

        var message = new TestMessageWithoutResponse(10);

        await handler.Handle(message);

        Assert.That(observations.MessagesFromMiddlewares, Is.EqualTo(new[] { message }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestMessageMiddleware<TestMessageWithoutResponse, UnitMessageResponse>) }));
    }

    [Test]
    public async Task GivenHandlerAndClientPipeline_WhenHandlerIsCalled_TransportTypesInPipelinesAreCorrect()
    {
        var services = new ServiceCollection();
        MessageTransportType? transportTypeFromClient = null;
        MessageTransportType? transportTypeFromHandler = null;

        _ = services.AddMessageHandlerDelegate(TestMessage.T, async (message, _, _) =>
        {
            await Task.Yield();
            return new(message.Payload + 1);
        }, pipeline => transportTypeFromHandler = pipeline.TransportType);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var message = new TestMessage(10);

        _ = await handler.WithPipeline(pipeline => transportTypeFromClient = pipeline.TransportType).Handle(message);

        Assert.That(transportTypeFromClient, Is.EqualTo(new MessageTransportType(ConquerorConstants.InProcessTransportName, MessageTransportRole.Sender)));
        Assert.That(transportTypeFromHandler, Is.EqualTo(new MessageTransportType(ConquerorConstants.InProcessTransportName, MessageTransportRole.Receiver)));
    }

    [Test]
    public async Task GivenHandlerAndClientPipeline_WhenPipelineIsBeingBuilt_MiddlewaresCanBeEnumerated()
    {
        var services = new ServiceCollection();

        _ = services.AddMessageHandlerDelegate(TestMessage.T,
                                               (message, _, _) => Task.FromResult<TestMessageResponse>(new(message.Payload + 1)),
                                               pipeline =>
                                               {
                                                   var middleware1 = new TestMessageMiddleware<TestMessage, TestMessageResponse>(new());
                                                   var middleware2 = new TestMessageMiddleware2<TestMessage, TestMessageResponse>(new());
                                                   _ = pipeline.Use(middleware1).Use(middleware2);

                                                   Assert.That(pipeline, Has.Count.EqualTo(2));
                                                   Assert.That(pipeline, Is.EqualTo(new IMessageMiddleware<TestMessage, TestMessageResponse>[] { middleware1, middleware2 }));
                                               });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>()
                              .For(TestMessage.T);

        var message = new TestMessage(10);

        _ = await handler.WithPipeline(pipeline =>
                         {
                             var middleware1 = new TestMessageMiddleware<TestMessage, TestMessageResponse>(new());
                             var middleware2 = new TestMessageMiddleware2<TestMessage, TestMessageResponse>(new());
                             _ = pipeline.Use(middleware1).Use(middleware2);

                             Assert.That(pipeline, Has.Count.EqualTo(2));
                             Assert.That(pipeline, Is.EqualTo(new IMessageMiddleware<TestMessage, TestMessageResponse>[] { middleware1, middleware2 }));
                         })
                         .Handle(message);
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase<TMessage, TResponse>(
        Action<IMessagePipeline<TMessage, TResponse>>? ConfigureHandlerPipeline,
        Action<IMessagePipeline<TMessage, TResponse>>? ConfigureClientPipeline,
        IReadOnlyCollection<(Type MiddlewareType, MessageTransportRole TransportRole)> ExpectedMiddlewareTypes,
        IReadOnlyCollection<MessageTransportRole> ExpectedTransportRolesFromPipelineBuilders)
        where TMessage : class, IMessage<TMessage, TResponse>;

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage(int Payload);

    public sealed record TestMessageResponse(int Payload);

    private sealed partial class TestMessageHandler(TestObservations observations) : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessage.IPipeline>>()?.Invoke(pipeline);
        }
    }

    [Message]
    public sealed partial record TestMessageWithoutResponse(int Payload);

    private sealed partial class TestMessageWithoutResponseHandler(TestObservations observations) : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessageWithoutResponse.IPipeline>>()?.Invoke(pipeline);
        }
    }

    private sealed partial class MultiTestMessageHandler(TestObservations observations) : TestMessage.IHandler,
                                                                                          TestMessageWithoutResponse.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessage.IPipeline>>()?.Invoke(pipeline);
        }

        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessageWithoutResponse.IPipeline>>()?.Invoke(pipeline);
        }
    }

    [Message<TestMessageResponse>]
    private partial record TestMessageBase(int PayloadBase);

    private sealed record TestMessageSub(int PayloadBase, int PayloadSub) : TestMessageBase(PayloadBase);

    private sealed partial class TestMessageBaseHandler(TestObservations observations) : TestMessageBase.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageBase message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(message.PayloadBase + 1);
        }

        public static void ConfigurePipeline(TestMessageBase.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessageBase.IPipeline>>()?.Invoke(pipeline);
        }
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed partial class TestMessageForAssemblyScanningHandler(TestObservations observations)
        : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessage.IPipeline>>()?.Invoke(pipeline);
        }
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed partial class TestMessageWithoutResponseForAssemblyScanningHandler(TestObservations observations)
        : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.MessagesFromHandlers.Add(message);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessageWithoutResponse.IPipeline>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestMessageMiddleware<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.MessagesFromMiddlewares.Add(ctx.Message);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    private sealed class TestMessageMiddleware2<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.MessagesFromMiddlewares.Add(ctx.Message);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    private sealed class TestMessageRetryMiddleware<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.MessagesFromMiddlewares.Add(ctx.Message);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            _ = await ctx.Next(ctx.Message, ctx.CancellationToken);
            return await ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestMessageMiddleware<TMessage, TResponse>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.MessagesFromMiddlewares.Add(ctx.Message);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var message = ctx.Message;

            if (message is TestMessage testMessage)
            {
                message = (TMessage)(object)new TestMessage(testMessage.Payload + 1);
            }

            var response = await ctx.Next(message, cancellationTokensToUse.CancellationTokens[1]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestMessageResponse testMessageResponse)
            {
                response = (TResponse)(object)new TestMessageResponse(testMessageResponse.Payload + 2);
            }

            return response;
        }
    }

    private sealed class MutatingTestMessageMiddleware2<TMessage, TResponse>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.MessagesFromMiddlewares.Add(ctx.Message);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var message = ctx.Message;

            if (message is TestMessage testMessage)
            {
                message = (TMessage)(object)new TestMessage(testMessage.Payload + 2);
            }

            var response = await ctx.Next(message, cancellationTokensToUse.CancellationTokens[2]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestMessageResponse testMessageResponse)
            {
                response = (TResponse)(object)new TestMessageResponse(testMessageResponse.Payload + 1);
            }

            return response;
        }
    }

    private sealed class ThrowingTestMessageMiddleware<TMessage, TResponse>(Exception exception) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    // only used as a marker for pipeline type check
    private sealed class DelegateMessageMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx) => throw new NotSupportedException();
    }

    public sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> MessagesFromHandlers { get; } = [];

        public List<object> MessagesFromMiddlewares { get; } = [];

        public List<object> ResponsesFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<Type?> HandlerTypesFromPipelineBuilders { get; } = [];

        public List<MessageTransportType> TransportTypesFromPipelineBuilders { get; } = [];

        public List<MessageTransportType> TransportTypesFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
