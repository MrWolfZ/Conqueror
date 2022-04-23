namespace Conqueror.CQS
{
    public interface IQueryPipelineBuilder
    {
        IQueryPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IQueryMiddleware;
        
        IQueryPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
    }
}
