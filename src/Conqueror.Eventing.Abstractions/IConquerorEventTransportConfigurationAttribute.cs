using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "this is a marker interface for attributes, so the name is fine")]
public interface IConquerorEventTransportConfigurationAttribute
{
}
