using System.Security.Claims;
using Conqueror.Middleware.Authorization.Messaging;

namespace Conqueror.Middleware.Authorization.Tests.Messaging;

public sealed partial class AuthorizationMessageMiddlewareTests
{
    [Test]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithoutConfiguration_WhenCallingHandler_CallSucceeds()
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        var handler = host.Resolve<IMessageSenders>()
                          .For(TestMessage.T)
                          .WithPipeline(p => p.UseAuthorization());

        var response = await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        Assert.That(response.Payload, Is.EqualTo(11));
    }

    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithChecks_WhenCallingHandler_CallSucceedsOrFailsBasedOnCheckResults(
        [Values("none", "syncSuccess", "syncFail", "asyncSuccess", "asyncFail")]
        string authenticationCheck,
        [Values("none", "syncSuccess", "syncFail", "asyncSuccess", "asyncFail")]
        string authorizationCheck)
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        var authenticationCheckName = $"authn {authenticationCheck}";
        var authorizationCheckName = $"authz {authorizationCheck}";

        var handler = host.Resolve<IMessageSenders>()
                          .For(TestMessage.T)
                          .WithPipeline(p => p.UseAuthorization(c =>
                          {
                              _ = authenticationCheck switch
                              {
                                  "none" => c,
                                  "syncSuccess" => c.AddAuthorizationCheck(authenticationCheckName, ctx => ctx.Success()),
                                  "syncFail" => c.AddAuthorizationCheck(authenticationCheckName, ctx => ctx.Unauthenticated("test failure")),
                                  "asyncSuccess" => c.AddAuthorizationCheck(authenticationCheckName, async ctx =>
                                  {
                                      await Task.Yield();
                                      ctx.CancellationToken.ThrowIfCancellationRequested();
                                      return ctx.Success();
                                  }),
                                  "asyncFail" => c.AddAuthorizationCheck(authenticationCheckName, async ctx =>
                                  {
                                      await Task.Yield();
                                      ctx.CancellationToken.ThrowIfCancellationRequested();
                                      return ctx.Unauthenticated(["test failure 1", "test failure 2"]);
                                  }),
                                  _ => throw new ArgumentOutOfRangeException(nameof(authenticationCheck), authenticationCheck, null),
                              };
                          }).ConfigureAuthorization(c =>
                          {
                              _ = authorizationCheck switch
                              {
                                  "none" => c,
                                  "syncSuccess" => c.AddAuthorizationCheck(authorizationCheckName, ctx => ctx.Success()),
                                  "syncFail" => c.AddAuthorizationCheck(authorizationCheckName, ctx => ctx.Unauthorized("test failure")),
                                  "asyncSuccess" => c.AddAuthorizationCheck(authorizationCheckName, async ctx =>
                                  {
                                      await Task.Yield();
                                      ctx.CancellationToken.ThrowIfCancellationRequested();
                                      return ctx.Success();
                                  }),
                                  "asyncFail" => c.AddAuthorizationCheck(authorizationCheckName, async ctx =>
                                  {
                                      await Task.Yield();
                                      ctx.CancellationToken.ThrowIfCancellationRequested();
                                      return ctx.Unauthorized(["test failure 1", "test failure 2"]);
                                  }),
                                  _ => throw new ArgumentOutOfRangeException(nameof(authorizationCheck), authorizationCheck, null),
                              };
                          }).ConfigureAuthorization(c =>
                          {
                              var expectedChecks = new List<string>().Concat(authenticationCheck is not "none" ? [authenticationCheckName] : [])
                                                                     .Concat(authorizationCheck is not "none" ? [authorizationCheckName] : []);

                              Assert.That(c.AuthorizationChecks.Select(check => check.Id), Is.EqualTo(expectedChecks));
                          }));

        if (authenticationCheck is not "syncFail" and not "asyncFail" && authorizationCheck is not "syncFail" and not "asyncFail")
        {
            var response = await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

            Assert.That(response.Payload, Is.EqualTo(11));
            return;
        }

        var expectedFailureReason = authenticationCheck is "syncFail" or "asyncFail"
            ? MessageFailedException.WellKnownReasons.Unauthenticated
            : MessageFailedException.WellKnownReasons.Unauthorized;

        string[] expectedFailureDetails = authenticationCheck is "syncFail" || (authenticationCheck is not "asyncFail" && authorizationCheck is "syncFail")
            ? ["test failure"]
            : ["test failure 1", "test failure 2"];

        await Assert.ThatAsync(() => handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken),
                               Throws.Exception.TypeOf<MessageAuthorizationFailedException>()
                                     .With.Property(nameof(MessageAuthorizationFailedException.WellKnownReason)).EqualTo(expectedFailureReason)
                                     .And.Matches<MessageAuthorizationFailedException>(e => e.Result.Details.SequenceEqual(expectedFailureDetails)));
    }

    [Test]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithFailingCheck_WhenRemovingFailingCheckAndCallingHandler_CallSucceeds()
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        var handler = host.Resolve<IMessageSenders>()
                          .For(TestMessage.T)
                          .WithPipeline(p => p.UseAuthorization(c => c.AddAuthorizationCheck("test", ctx => ctx.Unauthenticated("test")))
                                              .ConfigureAuthorization(c => c.RemoveAuthorizationCheck("test")

                                                                            // use return value of prior call to assert it is still the configuration
                                                                            .AddAuthorizationCheck("test2", ctx => ctx.Success())));

        var response = await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        Assert.That(response.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithFailingCheck_WhenRemovingMiddlewareAndCallingHandler_CallSucceeds()
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        var handler = host.Resolve<IMessageSenders>()
                          .For(TestMessage.T)
                          .WithPipeline(p => p.UseAuthorization(c => c.AddAuthorizationCheck("test", ctx => ctx.Unauthenticated("test")))
                                              .WithoutAuthorization()

                                              // use return value of prior call to assert it is still the pipeline
                                              .Use(ctx => ctx.Next(ctx.Message, ctx.CancellationToken)));

        var response = await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        Assert.That(response.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithCheck_WhenCallingHandler_CorrectContextIsPassedToCheck()
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        MessageAuthorizationContext<TestMessage, TestMessageResponse>? seenContext = null;

        var claimsPrincipal = new ClaimsPrincipal();

        using var scope = host.Host.Services.CreateScope();

        using var conquerorContext = scope.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.DownstreamContextData.Set("test-key", "test-value");

        using var d = conquerorContext.SetCurrentPrincipal(claimsPrincipal);

        var handler = scope.ServiceProvider
                           .GetRequiredService<IMessageSenders>()
                           .For(TestMessage.T)
                           .WithPipeline(p => p.UseAuthorization(c => c.AddAuthorizationCheck("test", ctx =>
                                                                       {
                                                                           seenContext = ctx;
                                                                           return ctx.Success();
                                                                       })

                                                                       // use return value of prior call to assert it is still the configuration
                                                                       .AddAuthorizationCheck("test2", ctx => ctx.Success())));

        var msg = new TestMessage { Payload = 10 };
        _ = await handler.Handle(msg, host.TestTimeoutToken);

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext.Message, Is.SameAs(msg));
        Assert.That(seenContext.ServiceProvider, Is.SameAs(scope.ServiceProvider));
        Assert.That(seenContext.ConquerorContext.DownstreamContextData.Get<string>("test-key"), Is.EqualTo("test-value"));
        Assert.That(seenContext.CurrentPrincipal, Is.SameAs(claimsPrincipal));
        Assert.That(seenContext.CancellationToken, Is.EqualTo(host.TestTimeoutToken));
    }

    [Message<TestMessageResponse>]
    private sealed partial class TestMessage
    {
        public required int Payload { get; init; }
    }

    private sealed record TestMessageResponse(int Payload);

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(message.Payload + 1);
        }
    }
}
