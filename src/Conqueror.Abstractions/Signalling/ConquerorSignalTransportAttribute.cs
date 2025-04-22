using System;

namespace Conqueror.Signalling;

/// <summary>
///     This base attribute type is used by all Conqueror signal transport
///     marker attributes. It is used by source generators to prevent conflicts when
///     multiple transport attributes are present for a single signal type.
/// </summary>
public abstract class ConquerorSignalTransportAttribute : Attribute;
