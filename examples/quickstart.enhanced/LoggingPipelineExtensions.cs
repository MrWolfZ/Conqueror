using Conqueror;

namespace Quickstart.Enhanced;

public static class LoggingPipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> WithIndentedJsonPayloadLogFormatting<
        TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.ConfigureLogging(c =>
        {
            c.MessagePayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson;
            c.ResponsePayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson;
        });
    }

    public static ISignalPipeline<TSignal> WithIndentedJsonPayloadLogFormatting<
        TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.ConfigureLogging(c =>
        {
            c.PayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson;
        });
    }

    public static IMessagePipeline<TMessage, TResponse> OmitResponsePayloadFromLogsInProduction<
        TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.ConfigureLogging(c =>
        {
            if (!pipeline.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                c.ResponsePayloadLoggingStrategy = PayloadLoggingStrategy.Omit;
            }
        });
    }

    public static IMessagePipeline<TMessage, TResponse>
        OmitResponsePayloadFromLogsForResponseMatching<TMessage, TResponse>(
            this IMessagePipeline<TMessage, TResponse> pipeline,
            Predicate<TResponse> predicate)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.ConfigureLogging(c =>
        {
            c.ResponsePayloadLoggingStrategyFactory = (_, resp) => predicate(resp)
                ? PayloadLoggingStrategy.Omit
                : c.ResponsePayloadLoggingStrategy;
        });
    }

    public static ISignalPipeline<TSignal> UseLoggingWithIndentedJson<
        TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.UseLogging(c =>
        {
            c.PayloadLoggingStrategy = PayloadLoggingStrategy.IndentedJson;
        });
    }
}
