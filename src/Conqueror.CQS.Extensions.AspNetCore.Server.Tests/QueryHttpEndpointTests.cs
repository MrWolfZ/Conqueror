using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
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
            _ = services.AddMvc().AddConquerorCQS();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestPostQueryHandlerWithoutPayload>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }
    }
}
