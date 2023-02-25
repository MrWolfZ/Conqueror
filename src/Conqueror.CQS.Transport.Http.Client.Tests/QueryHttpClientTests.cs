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
        private Func<HttpContext, Func<Task>, Task>? middleware;

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
        public async Task GivenSuccessfulHttpCallWithStringPayload_ReturnsQueryResponse()
        {
            const string testPayload = "an example test payload";

            var handler = ResolveOnClient<ITestQueryWithStringPayloadHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = testPayload }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(testPayload, result.Payload);
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

        [Test]
        public async Task GivenSuccessfulHttpCallWithCustomPath_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithCustomPathHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallWithCustomPath_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryWithCustomPathHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithVersion_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithVersionHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallWithVersion_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryWithVersionHandler>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithCustomHeaders_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestQueryWithCustomHeadersHandler>();

            var seenAuthorizationHeader = string.Empty;
            var seenTestHeaderValues = Array.Empty<string?>();

            middleware = (ctx, next) =>
            {
                seenAuthorizationHeader = ctx.Request.Headers.Authorization;
                seenTestHeaderValues = ctx.Request.Headers["test-header"];

                return next();
            };

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.AreEqual("Basic test", seenAuthorizationHeader);
            CollectionAssert.AreEquivalent(new[] { "value1", "value2" }, seenTestHeaderValues);
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallWithCustomHeaders_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<ITestPostQueryWithCustomHeadersHandler>();

            var seenAuthorizationHeader = string.Empty;
            var seenTestHeaderValues = Array.Empty<string?>();

            middleware = (ctx, next) =>
            {
                seenAuthorizationHeader = ctx.Request.Headers.Authorization;
                seenTestHeaderValues = ctx.Request.Headers["test-header"];

                return next();
            };

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.AreEqual("Basic test", seenAuthorizationHeader);
            CollectionAssert.AreEquivalent(new[] { "value1", "value2" }, seenTestHeaderValues);
        }

        [Test]
        public async Task GivenSuccessfulHttpCallForDelegateHandler_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<IQueryHandler<TestDelegateQuery, TestDelegateQueryResponse>>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenSuccessfulPostHttpCallForDelegateHandler_ReturnsQueryResponse()
        {
            var handler = ResolveOnClient<IQueryHandler<TestPostDelegateQuery, TestDelegateQueryResponse>>();

            var result = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers(o => o.QueryPathConvention = new TestHttpQueryPathConvention());
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddConquerorQueryHandler<TestPostQueryHandler>()
                        .AddConquerorQueryHandler<TestQueryWithStringPayloadHandler>()
                        .AddConquerorQueryHandler<TestQueryWithoutPayloadHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithoutPayloadHandler>()
                        .AddConquerorQueryHandler<TestQueryWithCollectionPayloadHandler>()
                        .AddConquerorQueryHandler<TestQueryWithComplexPayloadHandler>()
                        .AddConquerorQueryHandler<TestQueryWithComplexPayloadWithCollectionPropertyHandler>()
                        .AddConquerorQueryHandler<TestQueryWithOptionalPropertyHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithCustomSerializedPayloadTypeHandler>()
                        .AddConquerorQueryHandler<TestQueryWithCustomPathConventionHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithCustomPathConventionHandler>()
                        .AddConquerorQueryHandler<TestQueryWithCustomPathHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithCustomPathHandler>()
                        .AddConquerorQueryHandler<TestQueryWithVersionHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithVersionHandler>()
                        .AddConquerorQueryHandler<TestQueryWithCustomHeadersHandler>()
                        .AddConquerorQueryHandler<TestPostQueryWithCustomHeadersHandler>()
                        .AddConquerorQueryHandler<NonHttpTestQueryHandler>()
                        .AddConquerorQueryHandlerDelegate<TestDelegateQuery, TestDelegateQueryResponse>((command, _, _) => Task.FromResult(new TestDelegateQueryResponse { Payload = command.Payload + 1 }))
                        .AddConquerorQueryHandlerDelegate<TestPostDelegateQuery, TestDelegateQueryResponse>((command, _, _) => Task.FromResult(new TestDelegateQueryResponse { Payload = command.Payload + 1 }));
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQSHttpClientServices(o =>
            {
                _ = o.UseHttpClient(HttpClient);

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };

                o.QueryPathConvention = new TestHttpQueryPathConvention();
            });

            var baseAddress = new Uri("http://conqueror.test");

            _ = services.AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithStringPayloadHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithoutPayloadHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithCollectionPayloadHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithComplexPayloadHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithComplexPayloadWithCollectionPropertyHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithOptionalPropertyHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestPostQueryHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestPostQueryWithoutPayloadHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestPostQueryWithCustomSerializedPayloadTypeHandler>(b => b.UseHttp(baseAddress, o => o.JsonSerializerOptions = new()
                        {
                            Converters = { new TestPostQueryWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                            PropertyNameCaseInsensitive = true,
                        }))
                        .AddConquerorQueryClient<ITestQueryWithCustomPathConventionHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestPostQueryWithCustomPathConventionHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithCustomPathHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestPostQueryWithCustomPathHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithVersionHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestPostQueryWithVersionHandler>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<ITestQueryWithCustomHeadersHandler>(b => b.UseHttp(baseAddress, o =>
                        {
                            o.Headers.Authorization = new("Basic", "test");
                            o.Headers.Add("test-header", new[] { "value1", "value2" });
                        }))
                        .AddConquerorQueryClient<ITestPostQueryWithCustomHeadersHandler>(b => b.UseHttp(baseAddress, o =>
                        {
                            o.Headers.Authorization = new("Basic", "test");
                            o.Headers.Add("test-header", new[] { "value1", "value2" });
                        }))
                        .AddConquerorQueryClient<IQueryHandler<TestDelegateQuery, TestDelegateQueryResponse>>(b => b.UseHttp(baseAddress))
                        .AddConquerorQueryClient<IQueryHandler<TestPostDelegateQuery, TestDelegateQueryResponse>>(b => b.UseHttp(baseAddress));
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

            _ = app.Use((ctx, next) => middleware != null ? middleware(ctx, next) : next());

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
        public sealed record TestQueryWithStringPayload
        {
            public string Payload { get; init; } = string.Empty;
        }

        public sealed record TestQueryWithStringPayloadResponse
        {
            public string Payload { get; init; } = string.Empty;
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

        [HttpQuery(Path = "/api/testQueryWithCustomPath")]
        public sealed record TestQueryWithCustomPath
        {
            public int Payload { get; init; }
        }

        [HttpQuery(Version = "v2")]
        public sealed record TestQueryWithVersion
        {
            public int Payload { get; init; }
        }

        [HttpQuery]
        public sealed record TestQueryWithCustomHeaders
        {
            public int Payload { get; init; }
        }

        [HttpQuery]
        public sealed record TestDelegateQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestDelegateQueryResponse
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

        [HttpQuery(UsePost = true, Path = "/api/testPostQueryWithCustomPath")]
        public sealed record TestPostQueryWithCustomPath
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true, Version = "v2")]
        public sealed record TestPostQueryWithVersion
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQueryWithCustomHeaders
        {
            public int Payload { get; init; }
        }

        [HttpQuery(UsePost = true)]
        public sealed record TestPostDelegateQuery
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

        public interface ITestQueryWithStringPayloadHandler : IQueryHandler<TestQueryWithStringPayload, TestQueryWithStringPayloadResponse>
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

        public interface ITestQueryWithCustomPathHandler : IQueryHandler<TestQueryWithCustomPath, TestQueryResponse>
        {
        }

        public interface ITestQueryWithVersionHandler : IQueryHandler<TestQueryWithVersion, TestQueryResponse>
        {
        }

        public interface ITestQueryWithCustomHeadersHandler : IQueryHandler<TestQueryWithCustomHeaders, TestQueryResponse>
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

        public interface ITestPostQueryWithCustomPathHandler : IQueryHandler<TestPostQueryWithCustomPath, TestQueryResponse>
        {
        }

        public interface ITestPostQueryWithVersionHandler : IQueryHandler<TestPostQueryWithVersion, TestQueryResponse>
        {
        }

        public interface ITestPostQueryWithCustomHeadersHandler : IQueryHandler<TestPostQueryWithCustomHeaders, TestQueryResponse>
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

        public sealed class TestQueryWithStringPayloadHandler : ITestQueryWithStringPayloadHandler
        {
            public async Task<TestQueryWithStringPayloadResponse> ExecuteQuery(TestQueryWithStringPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload };
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

        public sealed class TestQueryWithCustomPathHandler : ITestQueryWithCustomPathHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithCustomPath query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryWithVersionHandler : ITestQueryWithVersionHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithVersion query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestQueryWithCustomHeadersHandler : ITestQueryWithCustomHeadersHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithCustomHeaders query, CancellationToken cancellationToken = default)
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

        public sealed class TestPostQueryWithCustomPathHandler : ITestPostQueryWithCustomPathHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithCustomPath query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestPostQueryWithVersionHandler : ITestPostQueryWithVersionHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithVersion query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return new() { Payload = query.Payload + 1 };
            }
        }

        public sealed class TestPostQueryWithCustomHeadersHandler : ITestPostQueryWithCustomHeadersHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestPostQueryWithCustomHeaders query, CancellationToken cancellationToken = default)
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
