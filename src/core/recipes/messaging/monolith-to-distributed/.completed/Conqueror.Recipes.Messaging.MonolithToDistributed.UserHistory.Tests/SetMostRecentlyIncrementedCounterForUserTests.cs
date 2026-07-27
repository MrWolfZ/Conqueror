namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Tests;

[TestFixture]
public class SetMostRecentlyIncrementedCounterForUserTests
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "testUser";

    [Test]
    public async Task WhenSettingMostRecentlyIncrementedCounterForUser_ThenUserHistoryIsUpdated()
    {
        await using var host = TestHost.Create();

        // send the message through the web app's HTTP API, just like the Counters
        // web app does in production
        await host.HttpMessageSenders.For(SetMostRecentlyIncrementedCounterForUser.T)
                  .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(host.HttpClient))
                  .Handle(new(TestUserId, TestCounterName));

        var storedCounterName = await host.ResolveOnServer<IUserHistoryReadRepository>().GetMostRecentlyIncrementedCounterByUserId(TestUserId);

        Assert.That(storedCounterName, Is.EqualTo(TestCounterName));
    }
}
