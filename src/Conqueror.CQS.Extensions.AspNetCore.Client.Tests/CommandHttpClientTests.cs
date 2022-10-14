using System;
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
    public sealed class CommandHttpClientTests : TestBase
    {
        private const string ErrorPayload = "{\"Message\":\"this is an error\"}";

        private int? customResponseStatusCode;

        [Test]
        public async Task GivenSuccessfulHttpCall_ReturnsCommandResponse()
        {
            var handler = ResolveOnClient<ITestCommandHandler>();

            var result = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenFailedHttpCall_ThrowsHttpCommandFailedException()
        {
            var handler = ResolveOnClient<ITestCommandHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithoutResponse_ReturnsNothing()
        {
            var handler = ResolveOnClient<ITestCommandWithoutResponseHandler>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);
        }

        [Test]
        public async Task GivenFailedHttpCallWithoutResponse_ThrowsHttpCommandFailedException()
        {
            var handler = ResolveOnClient<ITestCommandWithoutResponseHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithoutPayload_ReturnsResponse()
        {
            var handler = ResolveOnClient<ITestCommandWithoutPayloadHandler>();

            var result = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        [Test]
        public async Task GivenFailedHttpCallWithoutPayload_ThrowsHttpCommandFailedException()
        {
            var handler = ResolveOnClient<ITestCommandWithoutPayloadHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new(), CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithoutResponseWithoutPayload_ReturnsNothing()
        {
            var handler = ResolveOnClient<ITestCommandWithoutResponseWithoutPayloadHandler>();

            await handler.ExecuteCommand(new(), CancellationToken.None);
        }

        [Test]
        public async Task GivenFailedHttpCallWithoutResponseWithoutPayload_ThrowsHttpCommandFailedException()
        {
            var handler = ResolveOnClient<ITestCommandWithoutResponseWithoutPayloadHandler>();

            customResponseStatusCode = StatusCodes.Status402PaymentRequired;

            var ex = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new(), CancellationToken.None));

            Assert.IsNotNull(ex);
            Assert.AreEqual(customResponseStatusCode, (int?)ex?.StatusCode);
            Assert.IsTrue(ex?.Message.Contains(ErrorPayload));
            Assert.AreEqual(ErrorPayload, await ex!.Response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task GivenSuccessfulHttpCallWithCustomSerializedPayloadType_ReturnsCommandResponse()
        {
            var handler = ResolveOnClient<ITestCommandWithCustomSerializedPayloadTypeHandler>();

            var result = await handler.ExecuteCommand(new(new(10)), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Payload);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestCommandWithCustomSerializedPayloadTypePayloadJsonConverterFactory()); });

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandWithoutResponseHandler>()
                        .AddTransient<TestCommandWithoutPayloadHandler>()
                        .AddTransient<TestCommandWithoutResponseWithoutPayloadHandler>()
                        .AddTransient<NonHttpTestCommandHandler>()
                        .AddTransient<NonHttpTestCommandWithoutResponseHandler>()
                        .AddTransient<TestCommandWithCustomSerializedPayloadTypeHandler>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCqsHttpClientServices(o =>
            {
                o.HttpClientFactory = uri =>
                    throw new InvalidOperationException(
                        $"during tests all clients should be explicitly configured with the test http client; got request to create http client for base address '{uri}'");

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };
            });

            _ = services.AddConquerorCommandHttpClient<ITestCommandHandler>(_ => HttpClient)
                        .AddConquerorCommandHttpClient<ITestCommandWithoutResponseHandler>(_ => HttpClient)
                        .AddConquerorCommandHttpClient<ITestCommandWithoutPayloadHandler>(_ => HttpClient)
                        .AddConquerorCommandHttpClient<ITestCommandWithoutResponseWithoutPayloadHandler>(_ => HttpClient)
                        .AddConquerorCommandHttpClient<ITestCommandWithCustomSerializedPayloadTypeHandler>(_ => HttpClient, o => o.JsonSerializerOptions = new()
                        {
                            Converters = { new TestCommandWithCustomSerializedPayloadTypePayloadJsonConverterFactory() },
                            PropertyNameCaseInsensitive = true,
                        });
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
