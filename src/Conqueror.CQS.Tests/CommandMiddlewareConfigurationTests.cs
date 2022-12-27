namespace Conqueror.CQS.Tests
{
    public sealed class CommandMiddlewareConfigurationTests
    {
        [Test]
        public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestCommandMiddlewareConfiguration(10);

            _ = services.ConfigureCommandPipeline<TestCommandHandler>(pipeline => { _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration); });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestCommandMiddlewareConfiguration(10);
            var overwrittenConfiguration = new TestCommandMiddlewareConfiguration(20);

            _ = services.ConfigureCommandPipeline<TestCommandHandler>(pipeline =>
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(overwrittenConfiguration);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestCommandMiddlewareConfiguration(10);

            _ = services.ConfigureCommandPipeline<TestCommandHandler>(pipeline =>
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => c.Parameter += 10);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

            Assert.AreEqual(20, initialConfiguration.Parameter);
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestCommandMiddlewareConfiguration(10);

            _ = services.ConfigureCommandPipeline<TestCommandHandler>(pipeline =>
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => new(c.Parameter + 10));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Has.Count.EqualTo(1));

            Assert.AreEqual(20, observations.Configurations[0].Parameter);
        }

        [Test]
        public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandler>(pipeline =>
            {
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new TestCommandMiddlewareConfiguration(20)));
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => c.Parameter += 10));
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => new(c.Parameter + 10)));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);
        }

        [Test]
        public async Task GivenExternalPipelineConfigurationAndHandlerWithOwnPipelineConfiguration_ExternalConfigurationIsUsed()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithPipelineConfiguration>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithPipelineConfiguration>(pipeline =>
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new(20));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Has.Count.EqualTo(1));

            Assert.AreEqual(20, observations.Configurations[0].Parameter);
        }

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestCommandHandlerWithPipelineConfiguration : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => Assert.Fail("should never be called");
        }
        
        private sealed class TestCommandMiddlewareConfiguration
        {
            public TestCommandMiddlewareConfiguration(int parameter)
            {
                Parameter = parameter;
            }

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
                observations.Configurations.Add(ctx.Configuration);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<TestCommandMiddlewareConfiguration> Configurations { get; } = new();
        }
    }
}
