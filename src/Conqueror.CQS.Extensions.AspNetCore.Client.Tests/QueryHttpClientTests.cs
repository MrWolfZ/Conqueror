using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    public sealed class QueryHttpClientTests : TestBase
    {
        private const string ErrorPayload = "{\"Message\":\"this is an error\"}";

        private int? customResponseStatusCode;

        [Test]
        public async Task GivenSuccessfulHttpCall_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenFailedHttpCall_ThrowsHttpQueryFailedException()
        {
            var handler = ResolveOnClient<ITestQueryHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithoutPayload_ReturnsResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithoutPayloadHandler>();

            var result = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenFailedHttpCallWithoutPayload_ThrowsHttpQueryFailedException()
        {
            var handler = ResolveOnClient<ITestQueryWithoutPayloadHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new(), CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCall_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenFailedPostHttpCall_ThrowsHttpQueryFailedException()
        {
            var handler = ResolveOnClient<ITestPostQueryHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallWithoutPayload_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryWithoutPayloadHandler>();

            var result = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenFailedPostHttpCallWithoutPayload_ThrowsHttpQueryFailedException()
        {
            var handler = ResolveOnClient<ITestPostQueryWithoutPayloadHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new(), CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallWithCustomSerializedPayloadType_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryWithCustomSerializedPayloadTypeHandler>();

            var result = await handler.ExecuteQuery(new(new(10)), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithCollectionPayload_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<IQueryHandler<TestQueryWithCollectionPayload, TestQueryWithCollectionPayloadResponse>>();

            var result = await handler.ExecuteQuery(new() { Payload = new() { 10, 11 } }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Payload.Count);
            Assert.AreEqual(10, result.Payload[0]);
            Assert.AreEqual(11, result.Payload[1]);
            Assert.AreEqual(1, result.Payload[2]);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestPostQueryWithCustomSerializedPayloadTypePayloadJsonConverterFactory()); });

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestQueryWithoutPayloadHandler>()
                        .AddTransient<TestPostQueryWithoutPayloadHandler>()
                        .AddTransient<TestQueryWithCollectionPayloadHandler>()
                        .AddTransient<TestPostQueryWithCustomSerializedPayloadTypeHandler>()
                        .AddTransient<NonHttpTestQueryHandler>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.HttpClientFactory = _ => HttpClient;
                            o.JsonSerializerOptionsFactory = _ => new()
                            {
                                Converters = { new TestPostQueryWithCustomSerializedPayloadTypePayloadJsonConverterFactory() },
                                PropertyNameCaseInsensitive = true,
                            };
                        })
                        .AddQueryHttpClient<ITestQueryHandler>()
                        .AddQueryHttpClient<ITestQueryWithoutPayloadHandler>()
                        .AddQueryHttpClient<ITestQueryWithCollectionPayloadHandler>()
                        .AddQueryHttpClient<ITestPostQueryHandler>()
                        .AddQueryHttpClient<ITestPostQueryWithoutPayloadHandler>()
                        .AddQueryHttpClient<ITestPostQueryWithCustomSerializedPayloadTypeHandler>();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.Use(async (ctx, next) =>
            {
                if (customResponseStatusCode != null)
                {
                    ctx.Response.StatusCode = customResponseStatusCode.Value;
                    ctx.Response.ContentType = MediaTypeNames.Application.Json;
                    await using var streamWriter = new StreamWriter(ctx.Response.Body);
                    await streamWriter.WriteAsync(ErrorPayload);
                    return;
                }

                await next();
            });

            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }
    }
}
