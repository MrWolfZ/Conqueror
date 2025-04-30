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
              .WithPipeline(p => p.UseLogging(c =>
              {
                  // you can set the logger category (by default, it is the fully qualified type
                  // name of the handler for handler pipelines and the signal type for publisher
                  // pipelines)
                  c.LoggerCategoryFactory = _
                      => typeof(DoublingCounterIncrementedHandler).FullName!;

                  // You can hook into the logging stages to customize the log messages
                  c.PreExecutionHook = ctx =>
                  {
                      ctx.Logger.LogInformation(
                          "doubling increment of counter '{CounterName}'",
                          ctx.Message.CounterName);

                      return false; // return true here to let the default message be logged
                  };

                  c.PostExecutionHook = ctx =>
                  {
                      ctx.Logger.LogInformation(
                          "doubled increment of counter '{CounterName}', it is now {NewValue}",
                          ctx.Message.CounterName,
                          ctx.Response.NewCounterValue);

                      return false;
                  };
              }))

              // You can customize the transport which is used to send the message (e.g. sending it
              // via HTTP), but for demonstration we use the in-process transport (which already
              // happens by default)
              .WithTransport(b => b.UseInProcess())

              // The 'Handle' method is unique for each `IHandler`, so "Go to Implementation" in
              // your IDE will jump directly to your handler, enabling smooth code base navigation,
              // even across different projects and transports
              .Handle(
                  new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                  cancellationToken);
    }
}
