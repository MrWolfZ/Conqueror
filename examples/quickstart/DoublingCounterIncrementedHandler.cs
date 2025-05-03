using Conqueror;

namespace Quickstart;

internal sealed partial class DoublingCounterIncrementedHandler(
    IMessageSenders senders)
    : CounterIncremented.IHandler
{
    // Signal handlers support handling multiple signal types (by adding more `IHandler`
    // interfaces), so the pipeline configuration is generic and is reused for all signal types
    // (`typeof(T)` can be checked to customize the pipeline for a specific signal type)
    static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
        pipeline.Use(ctx =>
                {
                    // we are only interested in specific signals, so we skip the handler (and the
                    // rest of the pipeline) for all others
                    if (ctx.Signal is CounterIncremented { CounterName: "doubler" })
                    {
                        return ctx.Next(ctx.Signal, ctx.CancellationToken);
                    }

                    return Task.CompletedTask;
                })
                .Use(ctx =>
                {
                    // Below in the 'Handle' method we call 'IncrementCounterByAmount' again,
                    // which could lead to an infinite loop. Conqueror "flows" context data
                    // across different executions, which is useful here to handle a signal
                    // only once per HTTP request
                    if (ctx.ConquerorContext.ContextData.Get<bool>("doubled"))
                    {
                        return Task.CompletedTask;
                    }

                    ctx.ConquerorContext.ContextData.Set("doubled", true);

                    return ctx.Next(ctx.Signal, ctx.CancellationToken);
                })

                // Middlewares in the pipeline are executed in the order that they are added.
                // We add the logging middleware to the pipeline only after the prior two
                // middlewares to ensure that only signals which are not skipped get logged
                .UseLogging(o => o.PayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson);

    public async Task Handle(
        CounterIncremented signal,
        CancellationToken cancellationToken = default)
    {
        await senders
              .For(IncrementCounterByAmount.T)

              // Message senders can also have pipelines and use different transports. The exact
              // same middlewares like logging, validation, error handling, etc. can be used on
              // both senders/publishers and handlers
              .WithPipeline(p => p.UseLogging())
              .WithTransport(b => b.UseInProcess())

              // The 'Handle' method is unique for each `IHandler`, so "Go to Implementation" in
              // your IDE will jump directly to your handler, enabling smooth code base navigation,
              // even across different projects and transports
              .Handle(
                  new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                  cancellationToken);
    }
}
