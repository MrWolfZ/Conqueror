namespace Conqueror;

using Iterating;

[IteratorTransport(Prefix = "Core", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "used by source generator"
)]
public sealed class IteratorAttribute<TItem> : Attribute;
