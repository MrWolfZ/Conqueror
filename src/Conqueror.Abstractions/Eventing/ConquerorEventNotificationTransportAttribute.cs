using System;

namespace Conqueror.Eventing;

/// <summary>
///     This base attribute type is used by all Conqueror event notification transport
///     marker attributes. It is used by source generators to prevent conflicts when
///     multiple transport attributes are present for a single event notification type.
/// </summary>
public abstract class ConquerorEventNotificationTransportAttribute : Attribute;
