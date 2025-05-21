using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;

namespace Conqueror.Transport.Http.Tests.Messaging.Server;

[TestFixture]
public sealed class MessagingServerExecutionTests
{
    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ReturnsCorrectResponse<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await HttpTransportTestHost.Create(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        var targetUriBuilder = new UriBuilder
        {
            Host = "localhost",
            Path = testCase.FullPath,
        };

        if (testCase.QueryString is not null)
        {
            targetUriBuilder.Query = testCase.QueryString;
        }

        using var request = new HttpRequestMessage(new(testCase.HttpMethod), targetUriBuilder.Uri);

        using var content = testCase.Payload is not null ? CreateJsonStringContent(testCase.Payload) : new(string.Empty);

        if (testCase.HttpMethod != MethodGet)
        {
            request.Content = content;
        }

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            return;
        }

        await response.AssertStatusCode(testCase.SuccessStatusCode);
        var resultString = await response.Content.ReadAsStringAsync();

        Assert.That(resultString, Is.EqualTo(testCase.ResponsePayload));

        if (testCase.ResponseType is not null)
        {
            if (testCase.ResponseSerializer is { } rs)
            {
                var result = await rs.Deserialize(host.Host.Services, response.Content, host.TestTimeoutToken);
                Assert.That(result, Is.Not.Null.And.EqualTo(testCase.Response));
            }
            else if (testCase.JsonSerializerContext is { } jsc)
            {
                var result = JsonSerializer.Deserialize(resultString, testCase.ResponseType, jsc);
                Assert.That(result, Is.Not.Null.And.EqualTo(testCase.Response));
            }
            else
            {
                var jsonSerializerOptions = host.Resolve<IOptions<JsonOptions>>().Value.SerializerOptions;
                var result = JsonSerializer.Deserialize(resultString, testCase.ResponseType, jsonSerializerOptions);

                Assert.That(result, Is.Not.Null.And.EqualTo(testCase.Response));
            }
        }

        if (testCase.Message is TestMessageWithMiddleware or TestMessageWithMiddlewareWithoutResponse)
        {
            var seenTransportType = host.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportType?.IsHttp(), Is.True, $"transport type is {seenTransportType?.Name}");
            Assert.That(seenTransportType?.Role, Is.EqualTo(MessageTransportRole.Receiver));
        }
    }

    [Test]
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ConquerorContextContainsClaimsPrincipal()
    {
        ClaimsPrincipal? seenClaimsPrincipal = null;

        await using var host = await HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddMessageHandler<TestMessageHandler>()
                            .AddSingleton<FnToCallFromHandler>((_, p) =>
                            {
                                seenClaimsPrincipal = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCurrentPrincipalInternal();
                                return Task.CompletedTask;
                            });

                _ = services.AddRouting().AddMessageEndpoints();
            },
            app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        var targetUriBuilder = new UriBuilder
        {
            Host = "localhost",
            Path = "api/test",
        };

        using var request = new HttpRequestMessage(new("POST"), targetUriBuilder.Uri);

        using var content = CreateJsonStringContent("{\"payload\":10}");
        request.Content = content;

        var response = await host.HttpClient.SendAsync(request);
        await response.AssertSuccessStatusCode();

        Assert.That(seenClaimsPrincipal, Is.Not.Null);
    }

    private static StringContent CreateJsonStringContent(string content)
    {
        return new(content, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
    }
}
