namespace Conqueror.CQS.Tests;

public sealed class CommandMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenHandlerWithNoHandlerMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerWithoutResponseNoHandlerMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerWithSingleAppliedHandlerMiddleware_MiddlewareIsCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithSingleMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerWithSingleAppliedHandlerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithSingleMiddlewareWithParameter>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(10), CancellationToken.None);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenHandlerWithoutResponseWithSingleAppliedHandlerMiddleware_MiddlewareIsCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithSingleMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerWithoutResponseWithSingleAppliedHandlerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new(10), CancellationToken.None);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenHandlerWithMultipleAppliedHandlerMiddlewares_MiddlewaresAreCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerWithoutResponseWithMultipleAppliedHandlerMiddlewares_MiddlewaresAreCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithMultipleMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithCommandMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenHandlerWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Use<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Without<TestCommandMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Use<TestCommandMiddleware2>()
                                    .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
    }

    [Test]
    public async Task GivenHandlerWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Use<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Without<TestCommandMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Use<TestCommandMiddleware2>()
                                    .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>();
                    });

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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware2>()
                                    .Without<TestCommandMiddleware2>()
                                    .Use<TestCommandMiddleware2>();
                    });

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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                    .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>()
                                    .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new());
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithRetryMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandRetryMiddleware>()
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
    public async Task GivenHandlerWithPipelineConfigurationMethodWithoutPipelineConfigurationInterface_MiddlewaresAreNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithPipelineConfigurationWithoutPipelineConfigurationInterface>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var command = new TestCommand(10);

        _ = await handler.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.CommandsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

#if !NET7_0_OR_GREATER
    [Test]
    public void GivenHandlerWithPipelineConfigurationInterfaceWithoutPipelineConfigurationMethod_RegisteringHandlerThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<TestCommandHandlerWithPipelineConfigurationInterfaceWithoutConfigurationMethod>());
    }

    [Test]
    public void GivenHandlerWithPipelineConfigurationInterfaceWithInvalidPipelineConfigurationMethodReturnType_RegisteringHandlerThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<TestCommandHandlerWithPipelineConfigurationInterfaceWithInvalidConfigurationMethodReturnType>());
    }

    [Test]
    public void GivenHandlerWithPipelineConfigurationInterfaceWithInvalidPipelineConfigurationMethodParameters_RegisteringHandlerThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<TestCommandHandlerWithPipelineConfigurationInterfaceWithInvalidConfigurationMethodParameters>());
    }
#endif

    [Test]
    public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleMiddlewares>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleMutatingMiddlewares>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
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
        Assert.That(observations.CommandsFromHandlers, Is.EquivalentTo(new[] { command3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCommandWithoutResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
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
        Assert.That(observations.CommandsFromHandlers, Is.EquivalentTo(new[] { command3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleMutatingMiddlewares>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var response = await handler.ExecuteCommand(new(0), CancellationToken.None);

        var response1 = new TestCommandResponse(0);
        var response2 = new TestCommandResponse(1);
        var response3 = new TestCommandResponse(3);

        Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
        Assert.That(response, Is.EqualTo(response3));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleMutatingMiddlewares>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<MutatingTestCommandMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(0), tokens.CancellationTokens[0]);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

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
        Assert.That(observedInstances[1], Is.Not.SameAs(observedInstances[0]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[0]));
    }

    [Test]
    public async Task GivenPipelineWithoutResponseThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

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
        Assert.That(observedInstances[1], Is.Not.SameAs(observedInstances[0]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[0]));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => pipeline.Use<TestCommandMiddleware2>());

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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => pipeline.Use<TestCommandMiddleware2>());

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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

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

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithThrowingMiddleware>()
                    .AddConquerorCommandMiddleware<ThrowingTestCommandMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_MiddlewareIsCalledWithCommand()
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
                                                                                          },
                                                                                          pipeline => pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()))
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public async Task GivenHandlerDelegateWithoutResponseWithSingleAppliedMiddleware_MiddlewareIsCalledWithCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>(async (command, p, cancellationToken) =>
                                                                                    {
                                                                                        await Task.Yield();
                                                                                        var obs = p.GetRequiredService<TestObservations>();
                                                                                        obs.CommandsFromHandlers.Add(command);
                                                                                        obs.CancellationTokensFromHandlers.Add(cancellationToken);
                                                                                    },
                                                                                    pipeline => pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()))
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
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
    public void InvalidMiddlewares()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCommandMiddleware<TestCommandMiddlewareWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCommandMiddleware<TestCommandMiddlewareWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCommandMiddleware(new TestCommandMiddlewareWithMultipleInterfaces()));
    }

    private sealed record TestCommand(int Payload);

    private sealed record TestCommandResponse(int Payload);

    private sealed record TestCommandWithoutResponse(int Payload);

    private sealed class TestCommandHandlerWithSingleMiddleware : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithSingleMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(command.Payload + 1);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new());
        }
    }

    private sealed class TestCommandHandlerWithSingleMiddlewareWithParameter : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithSingleMiddlewareWithParameter(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(command.Payload + 1);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestCommandHandlerWithoutResponseWithSingleMiddleware : ICommandHandler<TestCommandWithoutResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithoutResponseWithSingleMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new());
        }
    }

    private sealed class TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter : ICommandHandler<TestCommandWithoutResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestCommandHandlerWithMultipleMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithMultipleMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                        .Use<TestCommandMiddleware2>();
        }
    }

    private sealed class TestCommandHandlerWithoutResponseWithMultipleMiddlewares : ICommandHandler<TestCommandWithoutResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithoutResponseWithMultipleMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                        .Use<TestCommandMiddleware2>();
        }
    }

    private sealed class TestCommandHandlerWithoutMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithoutMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<ICommandPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestCommandHandlerWithoutResponseWithoutMiddlewares : ICommandHandler<TestCommandWithoutResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithoutResponseWithoutMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<ICommandPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestCommandHandlerWithRetryMiddleware : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandRetryMiddleware>()
                        .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                        .Use<TestCommandMiddleware2>();
        }
    }

    private sealed class TestCommandHandlerWithMultipleMutatingMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithMultipleMutatingMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<MutatingTestCommandMiddleware>()
                        .Use<MutatingTestCommandMiddleware2>();
        }
    }

    private sealed class TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares : ICommandHandler<TestCommandWithoutResponse>, IConfigureCommandPipeline
    {
        private readonly TestObservations observations;

        public TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.CommandsFromHandlers.Add(command);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<MutatingTestCommandMiddleware>()
                        .Use<MutatingTestCommandMiddleware2>();
        }
    }

    private sealed class TestCommandHandlerWithPipelineConfigurationWithoutPipelineConfigurationInterface : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }

        // ReSharper disable once UnusedMember.Local
        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

#if !NET7_0_OR_GREATER
    private sealed class TestCommandHandlerWithPipelineConfigurationInterfaceWithoutConfigurationMethod : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }
    }

    private sealed class TestCommandHandlerWithPipelineConfigurationInterfaceWithInvalidConfigurationMethodReturnType : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }

        // ReSharper disable once UnusedMember.Local
        public static ICommandPipelineBuilder ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline;
    }

    private sealed class TestCommandHandlerWithPipelineConfigurationInterfaceWithInvalidConfigurationMethodParameters : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }

        // ReSharper disable once UnusedMember.Local
        public static string ConfigurePipeline(string pipeline) => pipeline;
    }
#endif

    private sealed class TestCommandHandlerWithThrowingMiddleware : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            _ = pipeline.Use<ThrowingTestCommandMiddleware, TestCommandMiddlewareConfiguration>(new());
        }
    }

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

    private sealed class TestCommandMiddlewareWithMultipleInterfaces : ICommandMiddleware<TestCommandMiddlewareConfiguration>,
                                                                       ICommandMiddleware
    {
        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class =>
            throw new InvalidOperationException("this middleware should never be called");

        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
            where TCommand : class =>
            throw new InvalidOperationException("this middleware should never be called");
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = new();

        public List<object> CommandsFromHandlers { get; } = new();

        public List<object> CommandsFromMiddlewares { get; } = new();

        public List<object> ResponsesFromMiddlewares { get; } = new();

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = new();

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
