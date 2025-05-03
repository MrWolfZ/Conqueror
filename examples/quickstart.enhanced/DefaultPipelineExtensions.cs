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
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Type? loggerCategoryType = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        // when calling the handler from the same process, the handler already logs the message,
        // so we don't need to log it again
        if (pipeline.TransportType.IsInProcess())
        {
            return pipeline;
        }

        return pipeline.UseLogging(c =>
                       {
                           if (loggerCategoryType is not null)
                           {
                               c.LoggerCategoryFactory = _ => loggerCategoryType.FullName ??
                                                              loggerCategoryType.Name;
                           }
                       })

                       // we can already validate the message payload before sending it to the
                       // remote handler to fail fast without incurring the cost of the remote
                       // call
                       .UseDataAnnotationValidation()
                       .WithIndentedJsonPayloadLogFormatting();
    }

    public static ISignalPipeline<TSignal> UseDefaultForPublisher<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Type? loggerCategoryType = null)
        where TSignal : class, ISignal<TSignal>
    {
        // when calling the handler from the same process, we don't want to log the payload
        if (pipeline.TransportType.IsInProcess())
        {
            return pipeline;
        }

        return pipeline.UseLogging(c =>
                       {
                           if (loggerCategoryType is not null)
                           {
                               c.LoggerCategoryFactory = _ => loggerCategoryType.FullName ??
                                                              loggerCategoryType.Name;
                           }
                       })
                       .WithIndentedJsonPayloadLogFormatting();
    }

    public static TIHandler WithDefaultSenderPipeline<TMessage, TResponse, TIHandler>(
        this IMessageHandler<TMessage, TResponse, TIHandler> handler,
        Type? loggerCategoryType = null)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return handler.WithPipeline(p => p.UseDefaultForSender(loggerCategoryType));
    }

    public static TIHandler WithDefaultPublisherPipeline<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler,
        Type? loggerCategoryType = null)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return handler.WithPipeline(p => p.UseDefaultForPublisher(loggerCategoryType));
    }
}
