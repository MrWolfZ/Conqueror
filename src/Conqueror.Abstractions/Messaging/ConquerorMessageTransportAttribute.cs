using System;

namespace Conqueror.Messaging;

/// <summary>
///     This base attribute type is used by all Conqueror transport marker
///     attributes. It is used by source generators to prevent conflicts when
///     multiple transport attributes are present for a single message type.
/// </summary>
public abstract class ConquerorMessageTransportAttribute : Attribute;
