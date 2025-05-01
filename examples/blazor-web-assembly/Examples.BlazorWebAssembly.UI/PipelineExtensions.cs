namespace Examples.BlazorWebAssembly.UI;

public static class PipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDefaultClientPipeline<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.UseLogging()
                       .UseTimeout(TimeSpan.FromMinutes(1))
                       .UseRetry();
    }

    public static TIHandler WithDefaultClientPipeline<TMessage, TResponse, TIHandler>(this IMessageHandler<TMessage, TResponse, TIHandler> handler)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return handler.WithPipeline(pipeline => pipeline.UseDefaultClientPipeline());
    }
}
