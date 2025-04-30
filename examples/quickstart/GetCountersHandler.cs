using Conqueror;

namespace Quickstart;

internal sealed partial class GetCountersHandler(
    CountersRepository repository)
    : GetCounters.IHandler
{
    public static void ConfigurePipeline(GetCounters.IPipeline pipeline) =>
        pipeline.UseLogging(c =>
        {
            // The pipeline has access to the service provider from the scope of the call to the
            // handler in case you need it to resolve some services
            var isDevelopment = pipeline.ServiceProvider
                                        .GetRequiredService<IHostEnvironment>()
                                        .IsDevelopment();

            // The logging middleware supports detailed configuration options. For example, like
            // here we can omit verbose output from the logs in production. Note that in a real
            // application you would wrap such logic into an extension method (leveraging
            // `ConfigureLogging`) to make it reusable across message types. And thanks to the
            // builder pattern, you could then call it simply like this:
            // `pipeline.UseLogging().OmitResponseFromLogsInProduction()`
            c.ResponsePayloadLoggingStrategy = isDevelopment
                ? PayloadLoggingStrategy.IndentedJson
                : PayloadLoggingStrategy.Omit;

            // You can also make the logging strategy dependent on the message or response
            // payloads, e.g. to omit confidential data from the logs
            c.ResponsePayloadLoggingStrategyFactory = (_, resp)
                => resp.Any(c => c.CounterName == "confidential")
                    ? PayloadLoggingStrategy.Omit
                    : c.ResponsePayloadLoggingStrategy;

            c.PostExecutionHook = ctx =>
            {
                if (ctx.Response.Any(c => c.CounterName == "confidential"))
                {
                    // log an additional explanation for why the response is omitted from the logs
                    ctx.Logger.LogInformation("response omitted because of confidential data");
                }

                return true; // let the default message be logged
            };
        });

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
