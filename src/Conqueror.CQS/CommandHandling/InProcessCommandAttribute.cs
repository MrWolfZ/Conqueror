using System;

namespace Conqueror.CQS.CommandHandling;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class InProcessCommandAttribute : Attribute;
