using System;
using Conqueror.Messaging;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[MessageTransport(Prefix = "Core", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute : Attribute;

// ReSharper disable once UnusedTypeParameter (used by source generator)
[MessageTransport(Prefix = "Core", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute<TResponse> : Attribute;
