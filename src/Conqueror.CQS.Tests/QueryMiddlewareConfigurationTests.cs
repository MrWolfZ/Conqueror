namespace Conqueror.CQS.Tests
{
    public sealed class QueryMiddlewareConfigurationTests
    {
        [Test]
        public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestQueryMiddlewareConfiguration(10);

            _ = services.ConfigureQueryPipeline<TestQueryHandler>(pipeline => { _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(initialConfiguration); });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestQueryMiddlewareConfiguration(10);
            var overwrittenConfiguration = new TestQueryMiddlewareConfiguration(20);

            _ = services.ConfigureQueryPipeline<TestQueryHandler>(pipeline =>
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(overwrittenConfiguration);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestQueryMiddlewareConfiguration(10);

            _ = services.ConfigureQueryPipeline<TestQueryHandler>(pipeline =>
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(c => c.Parameter += 10);
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

            Assert.AreEqual(20, initialConfiguration.Parameter);
        }

        [Test]
        public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var initialConfiguration = new TestQueryMiddlewareConfiguration(10);

            _ = services.ConfigureQueryPipeline<TestQueryHandler>(pipeline =>
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(initialConfiguration);

                _ = pipeline.Configure<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(c => new(c.Parameter + 10));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Has.Count.EqualTo(1));

            Assert.AreEqual(20, observations.Configurations[0].Parameter);
        }

        [Test]
        public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            _ = services.ConfigureQueryPipeline<TestQueryHandler>(pipeline =>
            {
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new TestQueryMiddlewareConfiguration(20)));
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(c => c.Parameter += 10));
                _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(c => new(c.Parameter + 10)));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);
        }

        [Test]
        public async Task GivenExternalPipelineConfigurationAndHandlerWithOwnPipelineConfiguration_ExternalConfigurationIsUsed()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithPipelineConfiguration>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            _ = services.ConfigureQueryPipeline<TestQueryHandlerWithPipelineConfiguration>(pipeline =>
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new(20));
            });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.Configurations, Has.Count.EqualTo(1));

            Assert.AreEqual(20, observations.Configurations[0].Parameter);
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestQueryHandlerWithPipelineConfiguration : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => Assert.Fail("should never be called");
        }
        
        private sealed class TestQueryMiddlewareConfiguration
        {
            public TestQueryMiddlewareConfiguration(int parameter)
            {
                Parameter = parameter;
            }

            public int Parameter { get; set; }
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            private readonly TestObservations observations;

            public TestQueryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.Configurations.Add(ctx.Configuration);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<TestQueryMiddlewareConfiguration> Configurations { get; } = new();
        }
    }
}
