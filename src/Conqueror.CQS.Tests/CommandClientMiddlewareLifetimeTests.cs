namespace Conqueror.CQS.Tests;

public abstract class CommandClientMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingClientCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ResolvingClientCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ResolvingClientReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
    }

    [Test]
    public async Task GivenMultipleTransientMiddlewares_ResolvingClientCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services,
                                                                              CreateTransport,
                                                                              p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedMiddlewares_ResolvingClientCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services,
                                                                              CreateTransport,
                                                                              p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 1, 1, 2, 2, 3, 3 }));
    }

    [Test]
    public async Task GivenMultipleSingletonMiddlewares_ResolvingClientReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services,
                                                                              CreateTransport,
                                                                              p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingClientReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services,
                                                                              CreateTransport,
                                                                              p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandRetryMiddleware>()
                                                                                  .Use<TestCommandMiddleware>()
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandRetryMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandRetryMiddleware>()
                                                                                  .Use<TestCommandMiddleware>()
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandRetryMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandRetryMiddleware>()
                                                                                  .Use<TestCommandMiddleware>()
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddConquerorCommandMiddleware<TestCommandRetryMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
    }

    [Test]
    public async Task GivenTransientTransportWithRetryMiddleware_EachRetryGetsNewTransportInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            b => b.ServiceProvider.GetRequiredService<TestCommandTransport>(),
                                                                            p => p.Use<TestCommandRetryMiddleware>()
                                                                                  .Use<TestCommandMiddleware>()
                                                                                  .Use<TestCommandMiddleware2>());

        _ = services.AddTransient<TestCommandTransport>()
                    .AddConquerorCommandMiddleware<TestCommandRetryMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.TransportInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services,
                                                                            CreateTransport,
                                                                            p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimesForHandlerWithoutResponse_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services,
                                                                      CreateTransport,
                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());
        AddCommandClient<ICommandHandler<TestCommandWithoutResponse>>(services, CreateTransport, p => p.Use<TestCommandMiddleware>());

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
        var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

        _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
        await handler4.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
        _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
        await handler7.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
    }

    protected abstract void AddCommandClient<THandler>(IServiceCollection services,
                                                       Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                       Action<ICommandPipelineBuilder>? configurePipeline = null)
        where THandler : class, ICommandHandler;

    private static ICommandTransportClient CreateTransport(ICommandTransportClientBuilder builder)
    {
        return new TestCommandTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed record TestCommand2;

    private sealed record TestCommandResponse2;

    private sealed record TestCommandWithoutResponse;

    private sealed class TestCommandMiddleware : ICommandMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestCommandMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandMiddleware2 : ICommandMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestCommandMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandRetryMiddleware : ICommandMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestCommandRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            _ = await ctx.Next(ctx.Command, ctx.CancellationToken);
            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandTransport : ICommandTransportClient
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestCommandTransport(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : class
        {
            await Task.Yield();

            invocationCount += 1;
            observations.TransportInvocationCounts.Add(invocationCount);

            if (typeof(TCommand) == typeof(TestCommand))
            {
                return (TResponse)(object)new TestCommandResponse();
            }

            if (typeof(TCommand) == typeof(TestCommand2))
            {
                return (TResponse)(object)new TestCommandResponse2();
            }

            return (TResponse)(object)UnitCommandResponse.Instance;
        }
    }

    private sealed class DependencyResolvedDuringMiddlewareExecution
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public DependencyResolvedDuringMiddlewareExecution(TestObservations observations)
        {
            this.observations = observations;
        }

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> TransportInvocationCounts { get; } = new();

        public List<int> InvocationCounts { get; } = new();

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = new();
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class CommandClientMiddlewareLifetimeWithSyncFactoryTests : CommandClientMiddlewareLifetimeTests
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
public sealed class CommandClientMiddlewareLifetimeWithAsyncFactoryTests : CommandClientMiddlewareLifetimeTests
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
