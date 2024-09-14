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
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Server),
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2>(),
                         null,
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2>(),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2>(),
                         null,
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware2>(),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware>(),
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware>()
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestCommandMiddleware>()
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestCommandRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestCommandRetryMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestCommandRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandRetryMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestCommandRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestCommandRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestCommandRetryMiddleware), CommandTransportRole.Client),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                             (typeof(TestCommandRetryMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware2), CommandTransportRole.Client),
                             (typeof(TestCommandRetryMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
                             (typeof(TestCommandMiddleware), CommandTransportRole.Server),
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
                        _ = pipeline.Use(new MutatingTestCommandMiddleware(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestCommandMiddleware2(obs, cancellationTokensToUse));
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
                    .AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline => pipeline.Use(new ThrowingTestCommandMiddleware(exception)));

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

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestCommandMiddleware(exception))).ExecuteCommand(new(10)));

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

        _ = await handler.WithPipeline(p => p.Use(new TestCommandMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .WithPipeline(p => p.Use(new TestCommandMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .ExecuteCommand(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware) }));
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
                    }, pipeline => pipeline.Use(new TestCommandMiddleware(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
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

    private sealed class TestCommandMiddleware(TestObservations observations) : ICommandMiddleware
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandMiddleware2(TestObservations observations) : ICommandMiddleware
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandRetryMiddleware(TestObservations observations) : ICommandMiddleware
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
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

    private sealed class MutatingTestCommandMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : ICommandMiddleware
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
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

    private sealed class MutatingTestCommandMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : ICommandMiddleware
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
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

    private sealed class ThrowingTestCommandMiddleware(Exception exception) : ICommandMiddleware
    {
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
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
            where TMiddleware : ICommandMiddleware
        {
            _ = commandPipelineImplementation.Use(middleware);
            return this;
        }

        public ICommandPipeline<TestCommand, TestCommandResponse> Without<TMiddleware>()
            where TMiddleware : ICommandMiddleware
        {
            _ = commandPipelineImplementation.Without<TMiddleware>();
            return this;
        }

        public ICommandPipeline<TestCommand, TestCommandResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
            where TMiddleware : ICommandMiddleware
        {
            _ = commandPipelineImplementation.Configure(configure);
            return this;
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
