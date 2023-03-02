using Conqueror.CQS.Common;

namespace Conqueror.CQS.Tests;

public abstract class CommandClientMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenClientWithNoMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport);

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenClientWithoutResponseNoMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport);

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        var command = new TestCommandWithoutResponse(10);

        await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenClientWithSingleAppliedMiddleware_MiddlewareIsCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithSingleAppliedMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 }));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(10), CancellationToken.None);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenClientWithoutResponseWithSingleAppliedMiddleware_MiddlewareIsCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        var command = new TestCommandWithoutResponse(10);

        await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithoutResponseWithSingleAppliedMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 }));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new(10), CancellationToken.None);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenClientWithMultipleAppliedMiddlewares_MiddlewaresAreCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithoutResponseWithMultipleAppliedMiddlewares_MiddlewaresAreCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                            .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        var command = new TestCommandWithoutResponse(10);

        await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithCommandMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Without<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>()
                                                                                  .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Without<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>()
                                                                                  .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware2>()
                                                                                  .Without<TestCommandMiddleware2>()
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>()
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandRetryMiddleware>()
                                                                                  .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandRetryMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command, command, command, command }));
        Assert.That(observations.MiddlewareTypes,
                    Is.EquivalentTo(new[]
                    {
                        typeof(TestCommandRetryMiddleware), typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2),
                    }));
    }

    [Test]
    public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteCommand(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<MutatingTestCommandMiddleware>()
                                                                                  .Use<MutatingTestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(0), CancellationToken.None);

        var command1 = new TestCommand(0);
        var command2 = new TestCommand(1);
        var command3 = new TestCommand(3);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command1, command2 }));
        Assert.That(observations.CommandsFromTransports, Is.EquivalentTo(new[] { command3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCommandWithoutResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<MutatingTestCommandMiddleware>()
                                                                            .Use<MutatingTestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new(0), CancellationToken.None);

        var command1 = new TestCommandWithoutResponse(0);
        var command2 = new TestCommandWithoutResponse(1);
        var command3 = new TestCommandWithoutResponse(3);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command1, command2 }));
        Assert.That(observations.CommandsFromTransports, Is.EquivalentTo(new[] { command3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<MutatingTestCommandMiddleware>()
                                                                                  .Use<MutatingTestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var response = await handler.ExecuteCommand(new(0), CancellationToken.None);

        var response1 = new TestCommandResponse(4);
        var response2 = new TestCommandResponse(5);
        var response3 = new TestCommandResponse(7);

        Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
        Assert.AreEqual(response3, response);
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<MutatingTestCommandMiddleware>()
                                                                                  .Use<MutatingTestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(0), tokens.CancellationTokens[0]);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromTransports, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => observedInstances.Add(p.ServiceProvider.GetRequiredService<TestService>()));

        _ = services.AddScoped<TestService>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler1.ExecuteCommand(new(10), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(10), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(10), CancellationToken.None);

        Assert.That(observedInstances, Has.Count.EqualTo(3));
        Assert.AreNotSame(observedInstances[0], observedInstances[1]);
        Assert.AreSame(observedInstances[0], observedInstances[2]);
    }

    [Test]
    public async Task GivenPipelineWithoutResponseThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => observedInstances.Add(p.ServiceProvider.GetRequiredService<TestService>()));

        _ = services.AddScoped<TestService>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        await handler1.ExecuteCommand(new(10), CancellationToken.None);
        await handler2.ExecuteCommand(new(10), CancellationToken.None);
        await handler3.ExecuteCommand(new(10), CancellationToken.None);

        Assert.That(observedInstances, Has.Count.EqualTo(3));
        Assert.AreNotSame(observedInstances[0], observedInstances[1]);
        Assert.AreSame(observedInstances[0], observedInstances[2]);
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware2>());

        _ = services.AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware2)));
    }

    [Test]
    public void GivenPipelineWithoutResponseThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware2>());

        _ = services.AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware2)));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

        _ = services.AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware)));
    }

    [Test]
    public void GivenPipelineWithoutResponseThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

        _ = services.AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware)));
    }

    [Test]
    public void GivenMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<ThrowingTestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

        _ = services.AddConquerorCommandMiddleware<ThrowingTestCommandMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

        Assert.AreSame(exception, thrownException);
    }

    protected abstract void AddCommandClient<THandler>(IServiceCollection services,
                                                       Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                       Action<ICommandPipelineBuilder>? configurePipeline = null)
        where THandler : class, ICommandHandler;

    private static ICommandTransportClient CreateTransport(ICommandTransportClientBuilder builder)
    {
        return new TestCommandTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
    }

    private sealed record TestCommand(int Payload);

    private sealed record TestCommandResponse(int Payload);

    private sealed record TestCommandWithoutResponse(int Payload);

    private sealed record TestCommandMiddlewareConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestCommandMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandMiddleware2 : ICommandMiddleware
    {
        private readonly TestObservations observations;

        public TestCommandMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandRetryMiddleware : ICommandMiddleware
    {
        private readonly TestObservations observations;

        public TestCommandRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            _ = await ctx.Next(ctx.Command, ctx.CancellationToken);
            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestCommandMiddleware : ICommandMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestCommandMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var command = ctx.Command;

            if (command is TestCommand testCommand)
            {
                command = (TCommand)(object)(testCommand with { Payload = testCommand.Payload + 1 });
            }

            if (command is TestCommandWithoutResponse testCommandWithoutResponse)
            {
                command = (TCommand)(object)(testCommandWithoutResponse with { Payload = testCommandWithoutResponse.Payload + 1 });
            }

            var response = await ctx.Next(command, cancellationTokensToUse.CancellationTokens[1]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestCommandResponse testCommandResponse)
            {
                response = (TResponse)(object)(testCommandResponse with { Payload = testCommandResponse.Payload + 2 });
            }

            return response;
        }
    }

    private sealed class MutatingTestCommandMiddleware2 : ICommandMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestCommandMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.CommandsFromMiddlewares.Add(ctx.Command);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var command = ctx.Command;

            if (command is TestCommand testCommand)
            {
                command = (TCommand)(object)(testCommand with { Payload = testCommand.Payload + 2 });
            }

            if (command is TestCommandWithoutResponse testCommandWithoutResponse)
            {
                command = (TCommand)(object)(testCommandWithoutResponse with { Payload = testCommandWithoutResponse.Payload + 2 });
            }

            var response = await ctx.Next(command, cancellationTokensToUse.CancellationTokens[2]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestCommandResponse testCommandResponse)
            {
                response = (TResponse)(object)(testCommandResponse with { Payload = testCommandResponse.Payload + 1 });
            }

            return response;
        }
    }

    private sealed class ThrowingTestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
    {
        private readonly Exception exception;

        public ThrowingTestCommandMiddleware(Exception exception)
        {
            this.exception = exception;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
            where TCommand : class
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestCommandTransport : ICommandTransportClient
    {
        private readonly TestObservations observations;

        public TestCommandTransport(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class
        {
            await Task.Yield();

            observations.CommandsFromTransports.Add(command);
            observations.CancellationTokensFromTransports.Add(cancellationToken);

            if (typeof(TCommand) == typeof(TestCommand))
            {
                return (TResponse)(object)new TestCommandResponse((command as TestCommand)!.Payload + 1);
            }

            return (TResponse)(object)UnitCommandResponse.Instance;
        }
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = new();

        public List<object> CommandsFromTransports { get; } = new();

        public List<object> CommandsFromMiddlewares { get; } = new();

        public List<object> ResponsesFromMiddlewares { get; } = new();

        public List<CancellationToken> CancellationTokensFromTransports { get; } = new();

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

        public List<object> ConfigurationFromMiddlewares { get; } = new();
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = new();
    }

    private sealed class TestService
    {
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class CommandClientMiddlewareFunctionalityWithSyncFactoryTests : CommandClientMiddlewareFunctionalityTests
{
    protected override void AddCommandClient<THandler>(IServiceCollection services,
                                                       Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                       Action<ICommandPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorCommandClient<THandler>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class CommandClientMiddlewareFunctionalityWithAsyncFactoryTests : CommandClientMiddlewareFunctionalityTests
{
    protected override void AddCommandClient<THandler>(IServiceCollection services,
                                                       Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                       Action<ICommandPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorCommandClient<THandler>(async b =>
                                                         {
                                                             await Task.Delay(1);
                                                             return transportClientFactory(b);
                                                         },
                                                         configurePipeline);
    }
}
