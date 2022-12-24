using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    public sealed class QueryHttpEndpointTests : TestBase
    {
        [Test]
        public async Task HttpQueryTest()
        {
            var response = await HttpClient.GetAsync("/api/queries/test?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestQueryResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpPostQueryTest()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/queries/testPost", new TestQuery { Payload = 10 });
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestQueryResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpQueryWithoutPayloadTest()
        {
            var response = await HttpClient.GetAsync("/api/queries/testQueryWithoutPayload");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestQueryResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpPostQueryWithoutPayloadTest()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/queries/testPostQueryWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestQueryResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestPostQueryHandlerWithoutPayload>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034
    
        [HttpQuery]
        public sealed record TestQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestQueryResponse
        {
            public int Payload { get; init; }
        }
        
        [HttpQuery]
        public sealed record TestQuery2;

        public sealed record TestQueryResponse2;
    
        [HttpQuery]
        public sealed record TestQueryWithoutPayload;
    
        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery
        {
            public int Payload { get; init; }
        }
    
        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithoutPayload;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
        {
        }

        public sealed class TestQueryHandler : ITestQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
        {
            public Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestPostQueryHandler : ITestPostQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestPostQueryHandlerWithoutPayload : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }
    }
}
