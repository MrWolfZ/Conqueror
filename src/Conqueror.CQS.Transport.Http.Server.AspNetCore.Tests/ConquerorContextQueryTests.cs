using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using Conqueror.CQS.Transport.Http.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Net.Http.Headers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [NonParallelizable]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public sealed class ConquerorContextQueryTests : TestBase
    {
        private static readonly Dictionary<string, string> ContextItems = new()
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "keyWith,Comma", "value" },
            { "key4", "valueWith,Comma" },
            { "keyWith=Equals", "value" },
            { "key6", "valueWith=Equals" },
        };

        private DisposableActivity? activity;

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenContextItems_ItemsAreReturnedInHeader(string path, string? postContent = null)
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            var response = postContent != null ? await HttpClient.PostAsync(path, content) : await HttpClient.GetAsync(path);
            await response.AssertSuccessStatusCode();

            var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

            Assert.IsTrue(exists);

            var receivedItems = ContextValueFormatter.Parse(values!);

            CollectionAssert.AreEquivalent(ContextItems, receivedItems);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenConquerorContextRequestHeader_ItemsAreReceivedByHandler(string path, string? postContent = null)
        {
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(ContextItems) } },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenInvalidConquerorContextRequestHeader_ReturnsBadRequest(string path, string? postContent = null)
        {
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorContextHeaderName, "foo=bar" } },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertStatusCode(HttpStatusCode.BadRequest);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenQueryIdHeader_CorrectIdIsObservedByHandler(string path, string? postContent = null)
        {
            const string testQueryId = "TestQueryId";
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorQueryIdHeaderName, testQueryId } },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedQueryIds = Resolve<TestObservations>().ReceivedQueryIds;

            CollectionAssert.AreEquivalent(new[] { testQueryId }, receivedQueryIds);
        }

        [Test]
        public async Task GivenQueryIdHeader_CorrectIdsAreObservedByHandlerAndNestedHandler()
        {
            const string testQueryId = "TestQueryId";
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers = { { HttpConstants.ConquerorQueryIdHeaderName, testQueryId } },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedQueryIds = Resolve<TestObservations>().ReceivedQueryIds;

            Assert.That(receivedQueryIds, Has.Count.EqualTo(2));
            Assert.AreEqual(testQueryId, receivedQueryIds[0]);
            Assert.AreNotEqual(testQueryId, receivedQueryIds[1]);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenTraceIdInConquerorHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandler(string path, string? postContent = null)
        {
            const string testTraceId = "TestTraceId";
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorTraceIdHeaderName, testTraceId } },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            CollectionAssert.AreEquivalent(new[] { testTraceId }, receivedTraceIds);
        }

        [Test]
        public async Task GivenTraceIdInConquerorHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandlerAndNestedHandler()
        {
            const string testTraceId = "TestTraceId";
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers =
                {
                    { HttpConstants.ConquerorTraceIdHeaderName, testTraceId },
                },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
            Assert.AreEqual(testTraceId, receivedTraceIds[0]);
            Assert.AreEqual(testTraceId, receivedTraceIds[1]);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenTraceIdInConquerorHeaderWithActiveActivity_IdFromActivityIsObservedByHandler(string path, string? postContent = null)
        {
            using var a = CreateActivity(nameof(GivenTraceIdInConquerorHeaderWithActiveActivity_IdFromActivityIsObservedByHandler));
            activity = a;

            const string testTraceId = "TestTraceId";
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorTraceIdHeaderName, testTraceId } },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            CollectionAssert.AreEquivalent(new[] { a.TraceId }, receivedTraceIds);
        }

        [Test]
        public async Task GivenTraceIdInConquerorHeaderWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler()
        {
            using var a = CreateActivity(nameof(GivenTraceIdInConquerorHeaderWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler));
            activity = a;

            const string testTraceId = "TestTraceId";
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers =
                {
                    { HttpConstants.ConquerorTraceIdHeaderName, testTraceId },
                },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
            Assert.AreEqual(a.TraceId, receivedTraceIds[0]);
            Assert.AreEqual(a.TraceId, receivedTraceIds[1]);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandler(string path, string? postContent = null)
        {
            const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers =
                {
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            CollectionAssert.AreEquivalent(new[] { testTraceId }, receivedTraceIds);
        }

        [Test]
        public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandlerAndNestedHandler()
        {
            const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers =
                {
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
            Assert.AreEqual(testTraceId, receivedTraceIds[0]);
            Assert.AreEqual(testTraceId, receivedTraceIds[1]);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler(string path, string? postContent = null)
        {
            using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler));
            activity = a;

            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers =
                {
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            CollectionAssert.AreEquivalent(new[] { a.TraceId }, receivedTraceIds);
        }

        [Test]
        public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler()
        {
            using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler));
            activity = a;

            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers =
                {
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
            Assert.AreEqual(a.TraceId, receivedTraceIds[0]);
            Assert.AreEqual(a.TraceId, receivedTraceIds[1]);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenTraceIdInTraceParentAndConquerorHeadersWithoutActiveActivity_IdFromTraceParentHeaderIsObservedByHandler(string path, string? postContent = null)
        {
            const string testTraceId = "TestTraceId";
            const string testParentTraceId = "80e1a2ed08e019fc1110464cfa66635c";
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers =
                {
                    { HttpConstants.ConquerorTraceIdHeaderName, testTraceId },
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            CollectionAssert.AreEquivalent(new[] { testParentTraceId }, receivedTraceIds);
        }

        [Test]
        public async Task GivenTraceIdInTraceParentAndConquerorHeadersWithoutActiveActivity_IdFromTraceParentHeaderObservedByHandlerAndNestedHandler()
        {
            const string testTraceId = "TestTraceId";
            const string testParentTraceId = "80e1a2ed08e019fc1110464cfa66635c";
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers =
                {
                    { HttpConstants.ConquerorTraceIdHeaderName, testTraceId },
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
            Assert.AreEqual(testParentTraceId, receivedTraceIds[0]);
            Assert.AreEqual(testParentTraceId, receivedTraceIds[1]);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/queries/testPost", "{\"payload\":10}")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenTraceIdInTraceParentAndConquerorHeadersWithActiveActivity_IdFromActivityIsObservedByHandler(string path, string? postContent = null)
        {
            using var a = CreateActivity(nameof(GivenTraceIdInTraceParentAndConquerorHeadersWithActiveActivity_IdFromActivityIsObservedByHandler));
            activity = a;

            const string testTraceId = "TestTraceId";
            using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
            using var msg = new HttpRequestMessage
            {
                Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers =
                {
                    { HttpConstants.ConquerorTraceIdHeaderName, testTraceId },
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
                Content = postContent != null ? content : null,
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            CollectionAssert.AreEquivalent(new[] { a.TraceId }, receivedTraceIds);
        }

        [Test]
        public async Task GivenTraceIdInTraceParentAndConquerorHeadersWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler()
        {
            using var a = CreateActivity(nameof(GivenTraceIdInTraceParentAndConquerorHeadersWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler));
            activity = a;

            const string testTraceId = "TestTraceId";
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
                Headers =
                {
                    { HttpConstants.ConquerorTraceIdHeaderName, testTraceId },
                    { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
                },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

            Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
            Assert.AreEqual(a.TraceId, receivedTraceIds[0]);
            Assert.AreEqual(a.TraceId, receivedTraceIds[1]);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
            applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

            _ = services.AddSingleton(applicationPartManager);

            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestQueryWithNestedQueryHandler>()
                        .AddTransient<NestedTestQueryHandler>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.Use(async (ctx, next) =>
            {
                if (activity is not null)
                {
                    _ = activity.Activity.Start();

                    try
                    {
                        await next();
                        return;
                    }
                    finally
                    {
                        activity.Activity.Stop();
                    }
                }

                await next();
            });

            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        private static DisposableActivity CreateActivity(string name)
        {
            var activitySource = new ActivitySource(name);

            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };

            ActivitySource.AddActivityListener(activityListener);

            var a = activitySource.CreateActivity(name, ActivityKind.Server)!;
            return new(a, activitySource, activityListener, a);
        }

        [HttpQuery]
        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        [HttpQuery]
        public sealed record TestQueryWithoutPayload;

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery;

        [HttpQuery]
        public sealed record TestQueryWithNestedQuery;

        public sealed record NestedTestQuery;

        public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryContextAccessor queryContextAccessor;
            private readonly TestObservations testObservations;

            public TestQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                    IQueryContextAccessor queryContextAccessor,
                                    TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.queryContextAccessor = queryContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryContextAccessor queryContextAccessor;
            private readonly TestObservations testObservations;

            public TestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                        IQueryContextAccessor queryContextAccessor,
                                        TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.queryContextAccessor = queryContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryContextAccessor queryContextAccessor;
            private readonly TestObservations testObservations;

            public TestQueryHandlerWithoutPayload(IConquerorContextAccessor conquerorContextAccessor,
                                                  IQueryContextAccessor queryContextAccessor,
                                                  TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.queryContextAccessor = queryContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestQueryWithNestedQueryHandler : IQueryHandler<TestQueryWithNestedQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryHandler<NestedTestQuery, TestQueryResponse> nestedHandler;
            private readonly IQueryContextAccessor queryContextAccessor;
            private readonly TestObservations testObservations;

            public TestQueryWithNestedQueryHandler(IQueryContextAccessor queryContextAccessor,
                                                   IConquerorContextAccessor conquerorContextAccessor,
                                                   IQueryHandler<NestedTestQuery, TestQueryResponse> nestedHandler,
                                                   TestObservations testObservations)
            {
                this.queryContextAccessor = queryContextAccessor;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestQueryWithNestedQuery query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return nestedHandler.ExecuteQuery(new(), cancellationToken);
            }
        }

        public sealed class NestedTestQueryHandler : IQueryHandler<NestedTestQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryContextAccessor queryContextAccessor;
            private readonly TestObservations testObservations;

            public NestedTestQueryHandler(IQueryContextAccessor queryContextAccessor,
                                          IConquerorContextAccessor conquerorContextAccessor,
                                          TestObservations testObservations)
            {
                this.queryContextAccessor = queryContextAccessor;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(NestedTestQuery query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestObservations
        {
            public List<string?> ReceivedQueryIds { get; } = new();

            public List<string?> ReceivedTraceIds { get; } = new();

            public bool ShouldAddItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();
        }

        [ApiController]
        private sealed class TestHttpQueryController : ControllerBase
        {
            [HttpGet("/api/custom/queries/test")]
            public Task<TestQueryResponse> ExecuteTestQuery([FromQuery] TestQuery query, CancellationToken cancellationToken)
            {
                return HttpQueryExecutor.ExecuteQuery<TestQuery, TestQueryResponse>(HttpContext, query, cancellationToken);
            }

            [HttpGet("/api/custom/queries/testQueryWithoutPayload")]
            public Task<TestQueryResponse> ExecuteTestQueryWithoutPayload(CancellationToken cancellationToken)
            {
                return HttpQueryExecutor.ExecuteQuery<TestQueryWithoutPayload, TestQueryResponse>(HttpContext, cancellationToken);
            }
        }

        private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public override string Name => nameof(TestControllerApplicationPart);

            public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestHttpQueryController).GetTypeInfo() };
        }

        private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpQueryController);
        }

        private sealed class DisposableActivity : IDisposable
        {
            private readonly IReadOnlyCollection<IDisposable> disposables;

            public DisposableActivity(Activity activity, params IDisposable[] disposables)
            {
                Activity = activity;
                this.disposables = disposables;
            }

            public Activity Activity { get; }

            public string TraceId => Activity.TraceId.ToString();

            public void Dispose()
            {
                foreach (var disposable in disposables.Reverse())
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
