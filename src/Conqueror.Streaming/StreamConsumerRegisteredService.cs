using System;

namespace Conqueror.Streaming;

public sealed record StreamConsumerRegisteredService(Type ItemType,
                                                     Type ConsumerType,
                                                     Type? CustomInterfaceType,
                                                     Action<IStreamConsumerPipelineBuilder>? ConfigurePipeline);
