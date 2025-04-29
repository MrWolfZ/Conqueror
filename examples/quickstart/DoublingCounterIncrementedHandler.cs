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

                // Middlewares in the pipeline are executed in the order that they are added in.
                // We add the logging middleware to the pipeline only after the prior two
                // middlewares to ensure that only signals which are not skipped get logged.
                // The `Configure...` extension methods for middlewares can be used to modify the
                // behavior of middlewares that were added earlier to a pipeline. A common pattern
                // is to define reusable pipelines that define the order of middlewares and then
                // use `Configure...` for a particular handler to modify the pipeline as necessary
                .UseLogging(o => o.PayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson);

    public async Task Handle(
        CounterIncremented signal,
        CancellationToken cancellationToken = default)
    {
        await senders
              .For(IncrementCounterByAmount.T)

              // When calling a message (or signal, etc.) handler, you can specify a sender
              // pipeline, which is executed before the message is sent via the configured
              // transport (and on the receiver the handler's own pipeline is then also executed)
              .WithPipeline(p => p.UseLogging(o =>
              {
                  o.PreExecutionHook = ctx =>
                  {
                      // Let's log a custom log message instead of Conqueror's default
                      ctx.Logger.LogInformation(
                          "doubling increment of counter '{CounterName}'",
                          ctx.Message.CounterName);
                      return false;
                  };

                  o.PostExecutionHook = ctx =>
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
              .Handle(new(signal.CounterName) { IncrementBy = signal.IncrementBy },
                      cancellationToken);
    }
}
