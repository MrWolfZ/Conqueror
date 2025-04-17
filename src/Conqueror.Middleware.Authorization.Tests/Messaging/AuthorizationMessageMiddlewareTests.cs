using System.Security.Claims;
using Conqueror.Middleware.Authorization.Messaging;

namespace Conqueror.Middleware.Authorization.Tests.Messaging;

public sealed partial class AuthorizationMessageMiddlewareTests
{
    [Test]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithoutConfiguration_WhenCallingHandler_CallSucceeds()
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        var handler = host.Resolve<IMessageClients>()
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

        var handler = host.Resolve<IMessageClients>()
                          .For(TestMessage.T)
                          .WithPipeline(p => p.UseAuthorization(c =>
                          {
                              _ = authenticationCheck switch
                              {
                                  "none" => c,
                                  "syncSuccess" => c.AddAuthorizationCheck(ctx => ctx.Success()),
                                  "syncFail" => c.AddAuthorizationCheck(ctx => ctx.Unauthenticated("test failure")),
                                  "asyncSuccess" => c.AddAuthorizationCheck(async (ctx, ct) =>
                                  {
                                      await Task.Yield();
                                      ct.ThrowIfCancellationRequested();
                                      return ctx.Success();
                                  }),
                                  "asyncFail" => c.AddAuthorizationCheck(async (ctx, ct) =>
                                  {
                                      await Task.Yield();
                                      ct.ThrowIfCancellationRequested();
                                      return ctx.Unauthenticated(["test failure 1", "test failure 2"]);
                                  }),
                                  _ => throw new ArgumentOutOfRangeException(nameof(authenticationCheck), authenticationCheck, null),
                              };
                          }).ConfigureAuthorization(c =>
                          {
                              _ = authorizationCheck switch
                              {
                                  "none" => c,
                                  "syncSuccess" => c.AddAuthorizationCheck(ctx => ctx.Success()),
                                  "syncFail" => c.AddAuthorizationCheck(ctx => ctx.Unauthorized("test failure")),
                                  "asyncSuccess" => c.AddAuthorizationCheck(async (ctx, ct) =>
                                  {
                                      await Task.Yield();
                                      ct.ThrowIfCancellationRequested();
                                      return ctx.Success();
                                  }),
                                  "asyncFail" => c.AddAuthorizationCheck(async (ctx, ct) =>
                                  {
                                      await Task.Yield();
                                      ct.ThrowIfCancellationRequested();
                                      return ctx.Unauthorized(["test failure 1", "test failure 2"]);
                                  }),
                                  _ => throw new ArgumentOutOfRangeException(nameof(authorizationCheck), authorizationCheck, null),
                              };
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
                               Throws.Exception.TypeOf<MessageAuthorizationFailedException<TestMessage>>()
                                     .With.Property(nameof(MessageAuthorizationFailedException<TestMessage>.WellKnownReason)).EqualTo(expectedFailureReason)
                                     .And.Matches<MessageAuthorizationFailedException<TestMessage>>(e => e.Result.Details.SequenceEqual(expectedFailureDetails)));
    }

    [Test]
    public async Task GivenHandlerWithAuthorizationMiddlewareWithFailingCheck_WhenRemovingMiddlewareCallingHandler_CallSucceeds()
    {
        await using var host = await AuthorizationMiddlewareTestHost.Create(services => services.AddMessageHandler<TestMessageHandler>());

        var handler = host.Resolve<IMessageClients>()
                          .For(TestMessage.T)
                          .WithPipeline(p => p.UseAuthorization(c => c.AddAuthorizationCheck(ctx => ctx.Unauthenticated("test")))
                                              .WithoutAuthorization());

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
                           .GetRequiredService<IMessageClients>()
                           .For(TestMessage.T)
                           .WithPipeline(p => p.UseAuthorization(c =>
                           {
                               _ = c.AddAuthorizationCheck(ctx =>
                               {
                                   seenContext = ctx;
                                   return ctx.Success();
                               });
                           }));

        var msg = new TestMessage { Payload = 10 };
        _ = await handler.Handle(msg, host.TestTimeoutToken);

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext.Message, Is.SameAs(msg));
        Assert.That(seenContext.ServiceProvider, Is.SameAs(scope.ServiceProvider));
        Assert.That(seenContext.ConquerorContext.DownstreamContextData.Get<string>("test-key"), Is.EqualTo("test-value"));
        Assert.That(seenContext.CurrentPrincipal, Is.SameAs(claimsPrincipal));
    }

    [Message<TestMessageResponse>]
    private sealed partial class TestMessage
    {
        public required int Payload { get; init; }
    }

    private sealed record TestMessageResponse(int Payload);

    private sealed class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(message.Payload + 1);
        }
    }
}
