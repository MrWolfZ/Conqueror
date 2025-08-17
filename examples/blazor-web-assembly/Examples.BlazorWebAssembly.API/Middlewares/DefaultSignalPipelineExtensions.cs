namespace Examples.BlazorWebAssembly.API.Middlewares;

public static class DefaultSignalPipelineExtensions
{
    public static ISignalPipeline<TSignal> UseDefault<TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal> =>
        pipeline.UseLogging();

    public static ISignalPipeline<TSignal> UseDefaultForPublisher<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Type? loggerCategoryType = null)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.UseLogging(c =>
        {
            if (loggerCategoryType is not null)
            {
                c.LoggerCategoryFactory = _ => loggerCategoryType.FullName ?? loggerCategoryType.Name;
            }
        });
    }

    public static TIHandler WithDefaultPublisherPipeline<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler,
        Type? loggerCategoryType = null)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler> =>
        handler.WithPipeline(p => p.UseDefaultForPublisher(loggerCategoryType));
}
