using System;

namespace Conqueror.Messaging;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class InProcessMessageAttribute : Attribute;
