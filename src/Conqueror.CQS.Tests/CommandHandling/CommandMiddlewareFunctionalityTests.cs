using Conqueror.CQS.CommandHandling;

namespace Conqueror.CQS.Tests.CommandHandling;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public to test case class")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "member order makes sense")]
public sealed class CommandMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenClientAndHandlerPipelines_WhenHandlerIsCalled_MiddlewaresAreCalledWithCommand(ConquerorMiddlewareFunctionalityTestCase testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline => testCase.ConfigureHandlerPipeline?.Invoke(pipeline));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        using var tokenSource = new CancellationTokenSource();

        var command = new TestCommand(10);

        _ = await handler.WithPipeline(pipeline => testCase.ConfigureClientPipeline?.Invoke(pipeline)).ExecuteCommand(command, tokenSource.Token);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(command, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => new CommandTransportType(InMemoryCommandTransportTypeExtensions.TransportName, t.TransportRole))));
    }

    [Test]
    [TestCaseSource(nameof(GenerateTestCasesWithoutResponse))]
    public async Task GivenClientAndHandlerPipelinesWithoutResponse_WhenHandlerIsCalled_MiddlewaresAreCalledWithCommand(ConquerorMiddlewareFunctionalityTestCaseWithoutResponse testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipeline<TestCommand>>>(pipeline => testCase.ConfigureHandlerPipeline?.Invoke(pipeline));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand>>();
        using var tokenSource = new CancellationTokenSource();

        var command = new TestCommand(10);

        await handler.WithPipeline(pipeline => testCase.ConfigureClientPipeline?.Invoke(pipeline)).ExecuteCommand(command, tokenSource.Token);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(command, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => new CommandTransportType(InMemoryCommandTransportTypeExtensions.TransportName, t.TransportRole))));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase> GenerateTestCases()
    {
        // no middleware
        yield return new(null,
                         null,
                         []);

        // single middleware
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2<TestCommand, TestCommandResponse>>(),
                         null,
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2<TestCommand, TestCommandResponse>>(),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2<TestCommand, TestCommandResponse>>(),
                         null,
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2<TestCommand, TestCommandResponse>>(),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware<TestCommand, TestCommandResponse>>(),
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware<TestCommand, TestCommandResponse>>()
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware<TestCommand, TestCommandResponse>>()
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestCommandRetryMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandRetryMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandRetryMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandRetryMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestCommandRetryMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestCommandRetryMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandRetryMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandRetryMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), CommandTransportRole.Client),
                             (typeof(TestCommandRetryMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), CommandTransportRole.Server),
                         ]);
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCaseWithoutResponse> GenerateTestCasesWithoutResponse()
    {
        foreach (var testCase in GenerateTestCases())
        {
            yield return new(testCase.ConfigureHandlerPipeline is { } c ? p => c(new CommandPipelineWithoutResponseAdapter(p)) : null,
                             testCase.ConfigureClientPipeline is { } c2 ? p => c2(new CommandPipelineWithoutResponseAdapter(p)) : null,
                             testCase.ExpectedMiddlewareTypes);
        }
    }

    [Test]
    public async Task GivenHandlerPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheCommandAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens)
                    .AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                        _ = pipeline.Use(new MutatingTestCommandMiddleware<TestCommand, TestCommandResponse>(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestCommandMiddleware2<TestCommand, TestCommandResponse>(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var response = await handler.ExecuteCommand(new(0), tokens.CancellationTokens[0]);

        var command1 = new TestCommand(0);
        var command2 = new TestCommand(1);
        var command3 = new TestCommand(3);

        var response1 = new TestCommandResponse(0);
        var response2 = new TestCommandResponse(1);
        var response3 = new TestCommandResponse(3);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command1, command2 }));
        Assert.That(observations.CommandsFromHandlers, Is.EquivalentTo(new[] { command3 }));

        Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
        Assert.That(response, Is.EqualTo(response3));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public void GivenHandlerPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(exception)
                    .AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline => pipeline.Use(new ThrowingTestCommandMiddleware<TestCommand, TestCommandResponse>(exception)));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteCommand(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenClientPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestCommandMiddleware<TestCommand, TestCommandResponse>(exception))).ExecuteCommand(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenMultipleClientPipelineConfigurations_WhenHandlerIsCalled_PipelinesAreExecutedInReverseOrder()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .WithPipeline(p => p.Use(new TestCommandMiddleware2<TestCommand, TestCommandResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .ExecuteCommand(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2<TestCommand, TestCommandResponse>), typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>) }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(async (command, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.CommandsFromHandlers.Add(command);
                        obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        return new(command.Payload + 1);
                    }, pipeline => pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>) }));
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase(
        Action<ICommandPipeline<TestCommand, TestCommandResponse>>? ConfigureHandlerPipeline,
        Action<ICommandPipeline<TestCommand, TestCommandResponse>>? ConfigureClientPipeline,
        IReadOnlyCollection<(Type MiddlewareType, CommandTransportRole TransportRole)> ExpectedMiddlewareTypes);

    public sealed record ConquerorMiddlewareFunctionalityTestCaseWithoutResponse(
        Action<ICommandPipeline<TestCommand>>? ConfigureHandlerPipeline,
        Action<ICommandPipeline<TestCommand>>? ConfigureClientPipeline,
        IReadOnlyCollection<(Type MiddlewareType, CommandTransportRole TransportRole)> ExpectedMiddlewareTypes);

    public sealed record TestCommand(int Payload);

    public sealed record TestCommandResponse(int Payload);

    private sealed class TestCommandHandler(TestObservations observations) : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestCommandResponse> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestCommandHandlerWithoutResponse(TestObservations observations) : ICommandHandler<TestCommand>
    {
        public async Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<ICommandPipeline<TestCommand>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestCommandMiddleware<TCommand, TResponse>(TestObservations observations) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandMiddleware2<TCommand, TResponse>(TestObservations observations) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandRetryMiddleware<TCommand, TResponse>(TestObservations observations) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            _ = await ctx.Next(ctx.Command, ctx.CancellationToken);
            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestCommandMiddleware<TCommand, TResponse>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var command = ctx.Command;

            if (command is TestCommand testCommand)
            {
                command = (TCommand)(object)new TestCommand(testCommand.Payload + 1);
            }

            var response = await ctx.Next(command, cancellationTokensToUse.CancellationTokens[1]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestCommandResponse testCommandResponse)
            {
                response = (TResponse)(object)new TestCommandResponse(testCommandResponse.Payload + 2);
            }

            return response;
        }
    }

    private sealed class MutatingTestCommandMiddleware2<TCommand, TResponse>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var command = ctx.Command;

            if (command is TestCommand testCommand)
            {
                command = (TCommand)(object)new TestCommand(testCommand.Payload + 2);
            }

            var response = await ctx.Next(command, cancellationTokensToUse.CancellationTokens[2]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestCommandResponse testCommandResponse)
            {
                response = (TResponse)(object)new TestCommandResponse(testCommandResponse.Payload + 1);
            }

            return response;
        }
    }

    private sealed class ThrowingTestCommandMiddleware<TCommand, TResponse>(Exception exception) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class CommandPipelineWithoutResponseAdapter(
        ICommandPipeline<TestCommand> commandPipelineImplementation) : ICommandPipeline<TestCommand, TestCommandResponse>
    {
        public IServiceProvider ServiceProvider => commandPipelineImplementation.ServiceProvider;

        public ICommandPipeline<TestCommand, TestCommandResponse> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : ICommandMiddleware<TestCommand, TestCommandResponse>
        {
            _ = commandPipelineImplementation.Use(new MiddlewareAdapter<TMiddleware>(middleware));
            return this;
        }

        public ICommandPipeline<TestCommand, TestCommandResponse> Without<TMiddleware>()
            where TMiddleware : ICommandMiddleware<TestCommand, TestCommandResponse>
        {
            _ = commandPipelineImplementation.Without<MiddlewareAdapter<TMiddleware>>();
            return this;
        }

        public ICommandPipeline<TestCommand, TestCommandResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
            where TMiddleware : ICommandMiddleware<TestCommand, TestCommandResponse>
        {
            _ = commandPipelineImplementation.Configure<MiddlewareAdapter<TMiddleware>>(m => configure(m.Wrapped));
            return this;
        }

        private sealed class MiddlewareAdapter<TMiddleware>(TMiddleware wrapped) : ICommandMiddleware<TestCommand, UnitCommandResponse>
            where TMiddleware : ICommandMiddleware<TestCommand, TestCommandResponse>
        {
            public TMiddleware Wrapped { get; } = wrapped;

            public async Task<UnitCommandResponse> Execute(CommandMiddlewareContext<TestCommand, UnitCommandResponse> ctx)
            {
                var wrappedCtx = new DefaultCommandMiddlewareContext<TestCommand, TestCommandResponse>(ctx.Command,
                                                                                                       async (c, t) =>
                                                                                                       {
                                                                                                           _ = await ctx.Next(c, t);
                                                                                                           return new(0);
                                                                                                       },
                                                                                                       ctx.ServiceProvider,
                                                                                                       ctx.ConquerorContext,
                                                                                                       ctx.TransportType,
                                                                                                       ctx.CancellationToken);

                _ = await Wrapped.Execute(wrappedCtx);
                return UnitCommandResponse.Instance;
            }
        }
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> CommandsFromHandlers { get; } = [];

        public List<object> CommandsFromMiddlewares { get; } = [];

        public List<object> ResponsesFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<CommandTransportType> TransportTypesFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
