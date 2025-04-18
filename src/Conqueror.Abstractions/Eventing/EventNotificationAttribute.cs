using System;
using Conqueror.Eventing;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventNotificationAttribute : ConquerorEventNotificationTransportAttribute;
