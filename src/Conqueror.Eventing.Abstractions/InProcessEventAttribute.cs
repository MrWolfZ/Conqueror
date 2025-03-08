using System;

namespace Conqueror;

[AttributeUsage(AttributeTargets.Class)]
public sealed class InProcessEventAttribute() : ConquerorEventTransportAttribute("in-process");
