using Conqueror;

namespace Quickstart.Enhanced;

public static class DefaultPipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDefault<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.UseLogging()
                       .UseDataAnnotationValidation()
                       .WithIndentedJsonPayloadLogFormatting();
    }

    public static ISignalPipeline<TSignal> UseDefault<TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.UseLogging()
                       .WithIndentedJsonPayloadLogFormatting();
    }

    public static IMessagePipeline<TMessage, TResponse> UseDefaultForSender<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        // when calling the handler from the same process, we don't want to log the payload
        if (pipeline.TransportType.IsInProcess())
        {
            return pipeline;
        }

        return pipeline.UseLogging()
                       .WithIndentedJsonPayloadLogFormatting();
    }

    public static ISignalPipeline<TSignal> UseDefaultForPublisher<TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        // when calling the handler from the same process, we don't want to log the payload
        if (pipeline.TransportType.IsInProcess())
        {
            return pipeline;
        }

        return pipeline.UseLogging()
                       .WithIndentedJsonPayloadLogFormatting();
    }

    public static TIHandler WithDefaultSenderPipeline<TMessage, TResponse, TIHandler>(
        this IMessageHandler<TMessage, TResponse, TIHandler> handler)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return handler.WithPipeline(p => p.UseDefaultForSender());
    }

    public static TIHandler WithDefaultPublisherPipeline<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return handler.WithPipeline(p => p.UseDefaultForPublisher());
    }
}
