using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute : Attribute;

// ReSharper disable once UnusedTypeParameter (used by source generator)
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute<TResponse> : Attribute;
