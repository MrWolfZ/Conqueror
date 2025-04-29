namespace Quickstart.Enhanced;

internal sealed partial class GetCountersHandler(
    CountersRepository repository)
    : GetCounters.IHandler
{
    public static void ConfigurePipeline(GetCounters.IPipeline pipeline) =>
        pipeline.UseDefault()
                .OmitResponsePayloadFromLogsInProduction()
                .OmitResponsePayloadFromLogsForResponseMatching(r => r.Any(c => c.CounterName ==
                                                                        "confidential"));

    public async Task<List<CounterValue>> Handle(
        GetCounters message,
        CancellationToken cancellationToken = default)
    {
        var allCounters = await repository.GetCounters();

        return allCounters.Where(p => message.Prefix is null || p.Key.StartsWith(message.Prefix))
                          .Select(p => new CounterValue(p.Key, p.Value))
                          .ToList();
    }
}
