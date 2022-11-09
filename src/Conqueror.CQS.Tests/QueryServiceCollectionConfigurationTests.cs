namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class QueryServiceCollectionConfigurationTests
    {
        [Test]
        public void GivenMultipleRegisteredIdenticalHandlerTypes_ConfiguringServiceCollectionDoesNotThrow()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<TestQueryHandler>();

            Assert.DoesNotThrow(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameQueryAndResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<DuplicateTestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameQueryAndDifferentResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<DuplicateTestQueryHandlerWithDifferentResponseType>();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenHandlerTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient(_ => new TestQueryHandler())
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenMiddlewareTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddTransient(_ => new TestQueryMiddleware())
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            Assert.DoesNotThrowAsync(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new(), CancellationToken.None));
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed record TestQueryResponse2;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestQueryHandlerWithDifferentResponseType : IQueryHandler<TestQuery, TestQueryResponse2>
        {
            public Task<TestQueryResponse2> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class TestQueryHandlerWithMiddleware : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.Use<TestQueryMiddleware>();
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware
        {
            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }
    }
}
