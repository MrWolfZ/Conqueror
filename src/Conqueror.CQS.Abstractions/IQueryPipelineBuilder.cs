﻿using System;

namespace Conqueror.CQS
{
    public interface IQueryPipelineBuilder
    {
        IQueryPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IQueryMiddleware;
        
        IQueryPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Without<TMiddleware>()
            where TMiddleware : IQueryMiddleware;
        
        IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
        
        IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : IQueryMiddleware<TConfiguration>;
    }
}
