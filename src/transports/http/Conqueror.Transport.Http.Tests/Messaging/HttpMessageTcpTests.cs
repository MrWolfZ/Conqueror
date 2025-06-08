using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;

namespace Conqueror.Transport.Http.Tests.Messaging;

[TestFixture]
public sealed class HttpMessageTcpTests
{
    private static readonly object[] HttpVersionTestCases =
    [
        new object[]
        {
            HttpProtocols.Http1,
            HttpVersion.Version11,
            HttpVersionPolicy.RequestVersionExact,
        },
        new object[]
        {
            HttpProtocols.Http2,
            HttpVersion.Version20,
            HttpVersionPolicy.RequestVersionExact,
        },
        new object[]
        {
            HttpProtocols.Http1AndHttp2,
            HttpVersion.Version20,
            HttpVersionPolicy.RequestVersionOrLower, // because we are not using HTTPS here, this will cause a downgrade to HTTP/1.1
        },
    ];

    [Test]
    [TestCaseSource(nameof(HttpVersionTestCases))]
    public async Task GivenWebAppListeningOnTcp_WhenCallingMessageEndpoint_ReturnsResponse(
        HttpProtocols serverProtocols,
        Version clientHttpVersion,
        HttpVersionPolicy clientVersionPolicy)
    {
        var builder = WebApplication.CreateBuilder();

        _ = builder.Logging.SetMinimumLevel(LogLevel.Trace);

        _ = builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(
                IPAddress.Loopback,
                0,
                listenOptions =>
                {
                    listenOptions.Protocols = serverProtocols;
                });
        });

        _ = builder.Services
                   .AddConquerorHttpServerAspNetCore()
                   .AddHttpMessageHandlerDelegate(
                       TestMessage.T,
                       (m, _, _) => new() { Payload = m.Payload + 1 });

        await using var app = builder.Build();

        _ = app.UseConquerorWellKnownErrorHandling();
        _ = app.MapMessageEndpoint(TestMessage.T);

        await app.StartAsync();

        Uri? listenAddress = null;

        if (app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>() is { } saf)
        {
            listenAddress = new(saf.Addresses.First());
        }

        Assert.That(listenAddress, Is.Not.Null);

        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .BuildServiceProvider();

        var handler = clientServices.GetRequiredService<IMessageSenders>()
                                    .For(TestMessage.T)
                                    .WithTransport(b => b.UseHttp(listenAddress)
                                                         .WithHttpVersion(clientHttpVersion, clientVersionPolicy));

        var response = await handler.Handle(new() { Payload = 10 });

        Assert.That(response.Payload, Is.EqualTo(11));

        await app.StopAsync();
    }
}
