using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Json;
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
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
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

        if (testCase.HttpMethod != MethodNames.Get)
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
    [Combinatorial]
    public async Task GivenTestHttpMessageWithDelegateHandler_WhenExecutingMessage_ReturnsCorrectResponse(
        [Values(true, false)] bool hasResponse,
        [Values(true, false)] bool isSync,
        [Values(true, false)] bool configuresPipeline,
        [Values(true, false)] bool configuresReceiver)
    {
        var middlewareWasCalled = false;
        var receiverWasConfigured = false;

        await using var host = await HttpTransportTestHost.Create(
            services =>
            {
                _ = (hasResponse, isSync, configuresPipeline, configuresReceiver) switch
                {
                    (false, false, false, false) => services.AddHttpMessageHandlerDelegate(TestMessageWithoutResponse.T, (_, _, _) => Task.CompletedTask),
                    (true, false, false, false) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => Task.FromResult(new TestMessageResponse())),
                    (false, true, false, false) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) =>
                        {
                        }),
                    (true, true, false, false) => services.AddHttpMessageHandlerDelegate(TestMessage.T, (_, _, _) => new()),
                    (false, false, true, false) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) => Task.CompletedTask,
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        })),
                    (true, false, true, false) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => Task.FromResult(new TestMessageResponse()),
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        })),
                    (false, true, true, false) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) =>
                        {
                        },
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        })),
                    (true, true, true, false) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => new(),
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        })),
                    (false, false, false, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) => Task.CompletedTask,
                        configureReceiver: r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (true, false, false, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => Task.FromResult(new TestMessageResponse()),
                        configureReceiver: r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (false, true, false, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) =>
                        {
                        },
                        configureReceiver: r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (true, true, false, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => new(),
                        configureReceiver: r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (false, false, true, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) => Task.CompletedTask,
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        }),
                        r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (true, false, true, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => Task.FromResult(new TestMessageResponse()),
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        }),
                        r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (false, true, true, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessageWithoutResponse.T,
                        (_, _, _) =>
                        {
                        },
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        }),
                        r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                    (true, true, true, true) => services.AddHttpMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _, _) => new(),
                        p => p.Use(ctx =>
                        {
                            middlewareWasCalled = true;

                            return ctx.Next(ctx.Message, ctx.CancellationToken);
                        }),
                        r =>
                        {
                            receiverWasConfigured = true;
                            _ = r.OmitFromApiDescription();
                        }),
                };

                _ = services.AddRouting().AddConquerorHttpServerAspNetCore();
            },
            app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        var targetUriBuilder = new UriBuilder
        {
            Host = "localhost",
            Path = hasResponse ? "api/test" : "api/testMessageWithoutResponse",
        };

        using var request = new HttpRequestMessage(new("POST"), targetUriBuilder.Uri);

        using var content = CreateJsonStringContent("{\"payload\":10}");
        request.Content = content;

        var response = await host.HttpClient.SendAsync(request);
        await response.AssertSuccessStatusCode();

        Assert.That(middlewareWasCalled, Is.EqualTo(configuresPipeline));
        Assert.That(receiverWasConfigured, Is.EqualTo(configuresReceiver));
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

                _ = services.AddRouting().AddConquerorHttpServerAspNetCore();
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
