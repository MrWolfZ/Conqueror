namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

[HttpMessage<GetMostRecentlyIncrementedCounterForUserResponse>(HttpMethod = "GET")]
public partial record GetMostRecentlyIncrementedCounterForUser(string UserId);

public record GetMostRecentlyIncrementedCounterForUserResponse(string? CounterName);

internal partial class GetMostRecentlyIncrementedCounterForUserHandler(IUserHistoryReadRepository repository)
    : GetMostRecentlyIncrementedCounterForUser.IHandler
{
    public static void ConfigurePipeline(GetMostRecentlyIncrementedCounterForUser.IPipeline pipeline) => pipeline.UseDefault();

    public async Task<GetMostRecentlyIncrementedCounterForUserResponse> Handle(GetMostRecentlyIncrementedCounterForUser message, CancellationToken cancellationToken = default)
    {
        var counterName = await repository.GetMostRecentlyIncrementedCounterByUserId(message.UserId);
        return new GetMostRecentlyIncrementedCounterForUserResponse(counterName);
    }
}
