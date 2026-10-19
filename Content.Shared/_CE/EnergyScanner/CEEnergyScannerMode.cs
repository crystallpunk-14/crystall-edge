using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.EnergyScanner;

/// <summary>
/// Which z-levels the energy scanner overlay shows. Cycled in declaration order by
/// <see cref="CEEnergyScannerCycleModeEvent"/>.
/// </summary>
[Serializable, NetSerializable]
public enum CEEnergyScannerMode : byte
{
    /// <summary>
    /// The viewer's level and the one below it.
    /// </summary>
    Below,

    /// <summary>
    /// Only the viewer's level.
    /// </summary>
    Current,

    /// <summary>
    /// The viewer's level and the one above it.
    /// </summary>
    Above,

    /// <summary>
    /// No pipe overlay at all.
    /// </summary>
    Off,
}

public sealed partial class CEEnergyScannerCycleModeEvent : InstantActionEvent;
