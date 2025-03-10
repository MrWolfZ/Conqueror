using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types

namespace Conqueror;

public interface IMessageHandler;

public interface IMessageHandler<TMessage> : IMessageHandler
    where TMessage : class, IMessage
{
    Task Handle(TMessage message, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(IMessagePipeline<TMessage> pipeline)
    {
        // by default, we use an empty pipeline
    }
}

public interface IMessageHandler<TMessage, TResponse> : IMessageHandler
    where TMessage : class, IMessage<TResponse>
{
    Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(IMessagePipeline<TMessage, TResponse> pipeline)
    {
        // by default, we use an empty pipeline
    }
}
