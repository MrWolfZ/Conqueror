namespace Conqueror;

using Messaging;

[MessageTransport(Prefix = "Core", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute : Attribute;

[MessageTransport(Prefix = "Core", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "used by source generator"
)]
public sealed class MessageAttribute<TResponse> : Attribute;
