using System;

namespace Conqueror.CQS
{
    public interface IQueryPipelineBuilder
    {
        IServiceProvider ServiceProvider { get; }
        
        IQueryPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IQueryMiddleware;
        
        IQueryPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Without<TMiddleware>()
            where TMiddleware : IQueryMiddleware;
        
        IQueryPipelineBuilder Without<TMiddleware, TConfiguration>()
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
    }
}
