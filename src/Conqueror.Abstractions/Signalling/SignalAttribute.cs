namespace Conqueror;

using Signalling;

[SignalTransport(Prefix = "Core", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SignalAttribute : Attribute;
