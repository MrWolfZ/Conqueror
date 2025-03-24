using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public delegate Task<TResponse> MessageHandlerFn<in TMessage, TResponse>(TMessage message,
                                                                         IServiceProvider serviceProvider,
                                                                         CancellationToken cancellationToken)
    where TMessage : class, IMessage<TResponse>;

public delegate Task MessageHandlerFn<in TMessage>(TMessage message,
                                                   IServiceProvider serviceProvider,
                                                   CancellationToken cancellationToken)
    where TMessage : class, IMessage<UnitMessageResponse>;

public delegate TResponse MessageHandlerSyncFn<in TMessage, out TResponse>(TMessage message,
                                                                           IServiceProvider serviceProvider,
                                                                           CancellationToken cancellationToken)
    where TMessage : class, IMessage<TResponse>;

public delegate void MessageHandlerSyncFn<in TMessage>(TMessage message,
                                                       IServiceProvider serviceProvider,
                                                       CancellationToken cancellationToken)
    where TMessage : class, IMessage<UnitMessageResponse>;
