using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using static Conqueror.ConquerorTransportHttpConstants;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;

namespace Conqueror.Transport.Http.Tests.Messaging.Server;

[TestFixture]
public sealed class MessagingServerExecutionTests
{
    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ReturnsCorrectResponse<TMessage, TResponse, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        await using var host = await HttpTransportTestHost.Create(
            services => services.RegisterMessageType<TMessage, TResponse, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse>(testCase));

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
            Assert.That(seenTransportType?.Role, Is.EqualTo(MessageTransportRole.Server));
        }
    }

    private static StringContent CreateJsonStringContent(string content)
    {
        return new(content, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
    }
}
