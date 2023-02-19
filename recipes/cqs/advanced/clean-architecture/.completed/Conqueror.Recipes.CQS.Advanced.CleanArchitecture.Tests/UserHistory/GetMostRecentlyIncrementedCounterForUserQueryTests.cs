using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;
using Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Tests.UserHistory;

[TestFixture]
public sealed class GetMostRecentlyIncrementedCounterForUserQueryTests : TestBase
{
    private const string TestCounterName = "testCounter";
    private const string TestUserId = "testUser";

    private IGetMostRecentlyIncrementedCounterForUserQueryHandler QueryClient => ResolveQueryClient<IGetMostRecentlyIncrementedCounterForUserQueryHandler>();

    [Test]
    public async Task GivenExistingUserHistory_WhenGettingMostRecentlyIncrementedCounterForUser_CounterNameIsReturned()
    {
        await ResolveOnServer<IUserHistoryWriteRepository>().SetMostRecentlyIncrementedCounter(TestUserId, TestCounterName);

        var response = await QueryClient.ExecuteQuery(new(TestUserId));

        Assert.That(response.CounterName, Is.EqualTo(TestCounterName));
    }

    [Test]
    public async Task GivenNonExistingUserHistory_WhenGettingMostRecentlyIncrementedCounterForUser_NullIsReturned()
    {
        var response = await QueryClient.ExecuteQuery(new(TestCounterName));

        Assert.That(response.CounterName, Is.Null);
    }
}
