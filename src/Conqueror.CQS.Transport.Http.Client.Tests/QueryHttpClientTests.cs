using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
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
            Assert.AreEqual(11, result.Payload.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithCollectionPayload_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithCollectionPayloadHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = new() { 10, 11 } }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Payload.Count);
            Assert.AreEqual(10, result.Payload[0]);
            Assert.AreEqual(11, result.Payload[1]);
            Assert.AreEqual(1, result.Payload[2]);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithComplexPayload_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithComplexPayloadHandler>();

            var result = await handler.ExecuteQuery(new(new(10)), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithComplexPayloadWithCollectionProperty_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithComplexPayloadWithCollectionPropertyHandler>();

            var result = await handler.ExecuteQuery(new(new(new() { 10, 11 })), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(22, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithOptionalPropertyPresent_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithOptionalPropertyHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10, OptionalPayload = 5 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(16, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithOptionalPropertyAbsent_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithOptionalPropertyHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithCustomPathConvention_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithCustomPathConventionHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallWithCustomPathConvention_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryWithCustomPathConventionHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.QueryPathConvention = new TestHttpQueryPathConvention());
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestQueryWithoutPayloadHandler>()
                        .AddTransient<TestPostQueryWithoutPayloadHandler>()
                        .AddTransient<TestQueryWithCollectionPayloadHandler>()
                        .AddTransient<TestQueryWithComplexPayloadHandler>()
                        .AddTransient<TestQueryWithComplexPayloadWithCollectionPropertyHandler>()
                        .AddTransient<TestQueryWithOptionalPropertyHandler>()
                        .AddTransient<TestPostQueryWithCustomSerializedPayloadTypeHandler>()
                        .AddTransient<TestQueryWithCustomPathConventionHandler>()
                        .AddTransient<TestPostQueryWithCustomPathConventionHandler>()
                        .AddTransient<NonHttpTestQueryHandler>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQSHttpClientServices(o =>
            {
                o.HttpClientFactory = uri =>
                    throw new InvalidOperationException(
                        $"during tests all clients should be explicitly configured with the test http client; got request to create http client for base address '{uri}'");

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };

                o.QueryPathConvention = new TestHttpQueryPathConvention();
            });

            _ = services.AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestQueryWithoutPayloadHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestQueryWithCollectionPayloadHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestQueryWithComplexPayloadHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestQueryWithComplexPayloadWithCollectionPropertyHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestQueryWithOptionalPropertyHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestPostQueryHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestPostQueryWithoutPayloadHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestPostQueryWithCustomSerializedPayloadTypeHandler>(b => b.UseHttp(HttpClient, o => o.JsonSerializerOptions = new()
                        {
                            Converters = { new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                            PropertyNameCaseInsensitive = true,
                        }))
                        .AddConquerorQueryClient<ITestQueryWithCustomPathConventionHandler>(b => b.UseHttp(HttpClient))
                        .AddConquerorQueryClient<ITestPostQueryWithCustomPathConventionHandler>(b => b.UseHttp(HttpClient));

            _ = services.FinalizeConquerorRegistrations();
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
        public sealed record TestQueryWithoutPayload;

        [HttpQuery]
        public sealed record TestQueryWithCollectionPayload
        {
            public List<int> Payload { get; init; } = new();
        }

        public sealed record TestQueryWithCollectionPayloadResponse
        {
            public List<int> Payload { get; init; } = new();
        }

        [HttpQuery]
        public sealed record TestQueryWithComplexPayload(TestQueryWithComplexPayloadPayload Payload);

        public sealed record TestQueryWithComplexPayloadPayload(int Payload);

        [HttpQuery]
        public sealed record TestQueryWithComplexPayloadWithCollectionProperty(TestQueryWithComplexPayloadWithCollectionPropertyPayload Payload);

        public sealed record TestQueryWithComplexPayloadWithCollectionPropertyPayload(List<int> Payload);

        [HttpQuery]
        public sealed record TestQueryWithOptionalProperty
        {
            public int Payload { get; init; }

            public int? OptionalPayload { get; init; }
        }

        [HttpQuery]
        public sealed record TestQueryWithCustomPathConvention
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithoutPayload;

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithCustomSerializedPayloadType(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

        public sealed record TestPostQueryWithCustomSerializedPayloadTypeResponse(TestPostQueryWithCustomSerializedPayloadTypePayload Payload);

        public sealed record TestPostQueryWithCustomSerializedPayloadTypePayload(int Payload);

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithCustomPathConvention
        {
            public int Payload { get; init; }
        }

        public sealed record NonHttpTestQuery
        {
            public int Payload { get; init; }
        }

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface ITestQueryWithoutPayloadHandler : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
        {
        }

        public interface ITestQueryWithCollectionPayloadHandler : IQueryHandler<TestQueryWithCollectionPayload, TestQueryWithCollectionPayloadResponse>
        {
        }

        public interface ITestQueryWithComplexPayloadHandler : IQueryHandler<TestQueryWithComplexPayload, TestQueryResponse>
        {
        }

        public interface ITestQueryWithComplexPayloadWithCollectionPropertyHandler : IQueryHandler<TestQueryWithComplexPayloadWithCollectionProperty, TestQueryResponse>
        {
        }

        public interface ITestQueryWithOptionalPropertyHandler : IQueryHandler<TestQueryWithOptionalProperty, TestQueryResponse>
        {
        }

        public interface ITestQueryWithCustomPathConventionHandler : IQueryHandler<TestQueryWithCustomPathConvention, TestQueryResponse>
        {
        }

        public interface ITestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
        {
        }

        public interface ITestPostQueryWithoutPayloadHandler : IQueryHandler<TestPostQueryWithoutPayload, TestQueryResponse>
        {
        }

        public interface ITestPostQueryWithCustomSerializedPayloadTypeHandler : IQueryHandler<TestPostQueryWithCustomSerializedPayloadType, TestPostQueryWithCustomSerializedPayloadTypeResponse>
        {
        }

        public interface ITestPostQueryWithCustomPathConventionHandler : IQueryHandler<TestPostQueryWithCustomPathConvention, TestQueryResponse>
        {
        }

        public interface INonHttpTestQueryHandler : IQueryHandler<NonHttpTestQuery, TestQueryResponse>
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

        public sealed class TestQueryWithoutPayloadHandler : ITestQueryWithoutPayloadHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestQueryWithCollectionPayloadHandler : ITestQueryWithCollectionPayloadHandler
        {
            public async Task<TestQueryWithCollectionPayloadResponse> ExecuteQuery(TestQueryWithCollectionPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = new(query.Payload) { 1 } };
            }
        }

        public sealed class TestQueryWithComplexPayloadHandler : ITestQueryWithComplexPayloadHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithComplexPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload.Payload + 1 };
            }
        }

        public sealed class TestQueryWithComplexPayloadWithCollectionPropertyHandler : ITestQueryWithComplexPayloadWithCollectionPropertyHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithComplexPayloadWithCollectionProperty query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload.Payload.Sum() + 1 };
            }
        }

        public sealed class TestQueryWithOptionalPropertyHandler : ITestQueryWithOptionalPropertyHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithOptionalProperty query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + (query.OptionalPayload ?? 0) + 1 };
            }
        }

        public sealed class TestQueryWithCustomPathConventionHandler : ITestQueryWithCustomPathConventionHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithCustomPathConvention query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
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

        public sealed class TestPostQueryWithoutPayloadHandler : ITestPostQueryWithoutPayloadHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = 11 };
            }
        }

        public sealed class TestPostQueryWithCustomSerializedPayloadTypeHandler : ITestPostQueryWithCustomSerializedPayloadTypeHandler
        {
            public async Task<TestPostQueryWithCustomSerializedPayloadTypeResponse> ExecuteQuery(TestPostQueryWithCustomSerializedPayloadType query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new(new(query.Payload.Payload + 1));
            }

            internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
            {
                public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestPostQueryWithCustomSerializedPayloadTypePayload);

                public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
                {
                    return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
                }
            }

            internal sealed class PayloadJsonConverter : JsonConverter<TestPostQueryWithCustomSerializedPayloadTypePayload>
            {
                public override TestPostQueryWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    return new(reader.GetInt32());
                }

                public override void Write(Utf8JsonWriter writer, TestPostQueryWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
                {
                    writer.WriteNumberValue(value.Payload);
                }
            }
        }

        public sealed class TestPostQueryWithCustomPathConventionHandler : ITestPostQueryWithCustomPathConventionHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithCustomPathConvention query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class NonHttpTestQueryHandler : INonHttpTestQueryHandler
        {
            public Task<TestQueryResponse> ExecuteQuery(NonHttpTestQuery query, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TestHttpQueryPathConvention : IHttpQueryPathConvention
        {
            public string? GetQueryPath(Type queryType, HttpQueryAttribute attribute)
            {
                if (queryType != typeof(TestQueryWithCustomPathConvention) && queryType != typeof(TestPostQueryWithCustomPathConvention))
                {
                    return null;
                }

                return $"/api/queries/{queryType.Name}FromConvention";
            }
        }
    }
}
