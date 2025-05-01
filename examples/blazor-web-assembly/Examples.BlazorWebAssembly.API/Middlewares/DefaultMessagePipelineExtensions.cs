namespace Examples.BlazorWebAssembly.API.Middlewares;

public static class DefaultMessagePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDefault<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.UseMetrics()
                       .UseLogging()
                       .UseAuthorization()
                       .UseValidation()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry()
                       .UseTransaction();
    }
}
