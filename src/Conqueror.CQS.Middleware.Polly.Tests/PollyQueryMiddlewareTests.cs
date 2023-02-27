using Polly;

namespace Conqueror.CQS.Middleware.Polly.Tests
{
    [TestFixture]
    public sealed class PollyQueryMiddlewareTests : TestBase
    {
        private Func<TestQuery, TestQueryResponse> handlerFn = _ => new();
        private Action<IQueryPipelineBuilder> configurePipeline = _ => { };

        [Test]
        public async Task GivenDefaultMiddlewareConfiguration_ExecutesHandlerWithoutModification()
        {
            var testQuery = new TestQuery();
            var expectedResponse = new TestQueryResponse();

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);
                return expectedResponse;
            };

            configurePipeline = pipeline => pipeline.Use<PollyQueryMiddleware, PollyQueryMiddlewareConfiguration>(new());

            var response = await Handler.ExecuteQuery(testQuery);

            Assert.AreSame(expectedResponse, response);
        }

        [Test]
        public void GivenDefaultMiddlewareConfiguration_ExecutesThrowingHandlerWithoutModification()
        {
            var testQuery = new TestQuery();
            var expectedException = new InvalidOperationException();

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);
                throw expectedException;
            };

            configurePipeline = pipeline => pipeline.Use<PollyQueryMiddleware, PollyQueryMiddlewareConfiguration>(new());

            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

            Assert.AreSame(expectedException, thrownException);
        }

        [Test]
        public async Task GivenConfigurationWithPolicy_ExecutesHandlerWithPolicy()
        {
            var testQuery = new TestQuery();
            var expectedResponse = new TestQueryResponse();

            var executionCount = 0;

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);

                executionCount += 1;

                if (executionCount < 2)
                {
                    throw new InvalidOperationException();
                }

                return expectedResponse;
            };

            var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

            configurePipeline = pipeline => pipeline.UsePolly(policy);

            var response = await Handler.ExecuteQuery(testQuery);

            Assert.AreSame(expectedResponse, response);
            Assert.AreEqual(2, executionCount);
        }

        [Test]
        public void GivenConfigurationWithPolicy_ExecutesThrowingHandlerWithPolicy()
        {
            var testQuery = new TestQuery();
            var expectedException = new InvalidOperationException();

            var executionCount = 0;

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);

                executionCount += 1;

                throw expectedException;
            };

            var policy = Policy.Handle<InvalidOperationException>().RetryAsync(3);

            configurePipeline = pipeline => pipeline.UsePolly(policy);

            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

            Assert.AreSame(expectedException, thrownException);
            Assert.AreEqual(4, executionCount);
        }

        [Test]
        public async Task GivenOverriddenConfigurationWithPolicy_ExecutesHandlerWithOverriddenPolicy()
        {
            var testQuery = new TestQuery();
            var expectedResponse = new TestQueryResponse();

            var executionCount = 0;

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);

                executionCount += 1;

                if (executionCount < 2)
                {
                    throw new InvalidOperationException();
                }

                return expectedResponse;
            };

            var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

            configurePipeline = pipeline => pipeline.UsePolly(Policy.NoOpAsync())
                                                    .ConfigurePollyPolicy(policy);

            var response = await Handler.ExecuteQuery(testQuery);

            Assert.AreSame(expectedResponse, response);
            Assert.AreEqual(2, executionCount);
        }

        [Test]
        public void GivenOverriddenConfigurationWithPolicy_ExecutesThrowingHandlerWithOverriddenPolicy()
        {
            var testQuery = new TestQuery();
            var expectedException = new InvalidOperationException();

            var executionCount = 0;

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);

                executionCount += 1;

                throw expectedException;
            };

            var policy = Policy.Handle<InvalidOperationException>().RetryAsync(3);

            configurePipeline = pipeline => pipeline.UsePolly(Policy.NoOpAsync())
                                                    .ConfigurePollyPolicy(policy);

            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

            Assert.AreSame(expectedException, thrownException);
            Assert.AreEqual(4, executionCount);
        }

        [Test]
        public void GivenRemovedPollyMiddleware_ExecutesHandlerWithoutModification()
        {
            var testQuery = new TestQuery();
            var expectedException = new InvalidOperationException();

            var executionCount = 0;

            handlerFn = query =>
            {
                Assert.AreSame(testQuery, query);

                executionCount += 1;

                if (executionCount < 2)
                {
                    throw expectedException;
                }

                return new();
            };

            var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

            configurePipeline = pipeline => pipeline.UsePolly(policy)
                                                    .WithoutPolly();

            var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

            Assert.AreSame(expectedException, thrownException);
            Assert.AreEqual(1, executionCount);
        }

        private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQSPollyMiddlewares()
                        .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(
                            async (query, _, _) =>
                            {
                                await Task.Yield();
                                return handlerFn(query);
                            },
                            pipeline => configurePipeline(pipeline));
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;
    }
}
