using System;
using Conqueror.Signalling;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SignalAttribute : ConquerorSignalTransportAttribute;
